mod support;

use std::alloc::{GlobalAlloc, Layout, System};
use std::env;
use std::sync::atomic::{AtomicU64, Ordering};
use std::time::Instant;

use support::{percentile, BenchTree, OK};

struct CountingAllocator;

static ALLOC_CALLS: AtomicU64 = AtomicU64::new(0);
static ALLOC_BYTES: AtomicU64 = AtomicU64::new(0);
static DEALLOC_CALLS: AtomicU64 = AtomicU64::new(0);
static DEALLOC_BYTES: AtomicU64 = AtomicU64::new(0);
static REALLOC_CALLS: AtomicU64 = AtomicU64::new(0);

#[global_allocator]
static GLOBAL_ALLOCATOR: CountingAllocator = CountingAllocator;

unsafe impl GlobalAlloc for CountingAllocator {
    unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
        let ptr = unsafe { System.alloc(layout) };
        if !ptr.is_null() {
            ALLOC_CALLS.fetch_add(1, Ordering::Relaxed);
            ALLOC_BYTES.fetch_add(layout.size() as u64, Ordering::Relaxed);
        }
        ptr
    }

    unsafe fn alloc_zeroed(&self, layout: Layout) -> *mut u8 {
        let ptr = unsafe { System.alloc_zeroed(layout) };
        if !ptr.is_null() {
            ALLOC_CALLS.fetch_add(1, Ordering::Relaxed);
            ALLOC_BYTES.fetch_add(layout.size() as u64, Ordering::Relaxed);
        }
        ptr
    }

    unsafe fn dealloc(&self, ptr: *mut u8, layout: Layout) {
        DEALLOC_CALLS.fetch_add(1, Ordering::Relaxed);
        DEALLOC_BYTES.fetch_add(layout.size() as u64, Ordering::Relaxed);
        unsafe { System.dealloc(ptr, layout) };
    }

    unsafe fn realloc(&self, ptr: *mut u8, layout: Layout, new_size: usize) -> *mut u8 {
        let new_ptr = unsafe { System.realloc(ptr, layout, new_size) };
        if !new_ptr.is_null() {
            REALLOC_CALLS.fetch_add(1, Ordering::Relaxed);
            DEALLOC_BYTES.fetch_add(layout.size() as u64, Ordering::Relaxed);
            ALLOC_BYTES.fetch_add(new_size as u64, Ordering::Relaxed);
        }
        new_ptr
    }
}

#[derive(Debug, Clone, Copy)]
struct Config {
    nodes: usize,
    samples: usize,
    warmup: usize,
}

impl Default for Config {
    fn default() -> Self {
        Self {
            nodes: 100,
            samples: 200,
            warmup: 20,
        }
    }
}

impl Config {
    fn parse() -> Self {
        let mut config = Self::default();
        let mut args = env::args().skip(1);
        while let Some(arg) = args.next() {
            match arg.as_str() {
                "--nodes" => config.nodes = parse_usize(&mut args, &arg),
                "--samples" => config.samples = parse_usize(&mut args, &arg),
                "--warmup" => config.warmup = parse_usize(&mut args, &arg),
                "--bench" => {}
                "--help" | "-h" => {
                    println!(
                        "usage: cargo bench --bench allocation_profile -- \
                         [--nodes N] [--samples N] [--warmup N]"
                    );
                    std::process::exit(0);
                }
                _ => panic!("unknown argument: {arg}"),
            }
        }
        assert!(config.nodes >= 2, "benchmark requires at least 2 nodes");
        assert!(config.samples > 0, "benchmark requires at least 1 sample");
        config
    }
}

fn parse_usize(args: &mut impl Iterator<Item = String>, arg: &str) -> usize {
    args.next()
        .unwrap_or_else(|| panic!("missing value for {arg}"))
        .parse::<usize>()
        .unwrap_or_else(|_| panic!("invalid integer value for {arg}"))
}

#[derive(Debug, Clone, Copy)]
struct AllocationSample {
    elapsed_ns: u128,
    alloc_calls: u64,
    alloc_bytes: u64,
    dealloc_calls: u64,
    dealloc_bytes: u64,
    realloc_calls: u64,
}

fn reset_counters() {
    ALLOC_CALLS.store(0, Ordering::Relaxed);
    ALLOC_BYTES.store(0, Ordering::Relaxed);
    DEALLOC_CALLS.store(0, Ordering::Relaxed);
    DEALLOC_BYTES.store(0, Ordering::Relaxed);
    REALLOC_CALLS.store(0, Ordering::Relaxed);
}

fn profile(tree: &BenchTree) -> AllocationSample {
    reset_counters();
    let start = Instant::now();
    let status = tree.compute();
    let elapsed_ns = start.elapsed().as_nanos();
    let sample = AllocationSample {
        elapsed_ns,
        alloc_calls: ALLOC_CALLS.load(Ordering::Relaxed),
        alloc_bytes: ALLOC_BYTES.load(Ordering::Relaxed),
        dealloc_calls: DEALLOC_CALLS.load(Ordering::Relaxed),
        dealloc_bytes: DEALLOC_BYTES.load(Ordering::Relaxed),
        realloc_calls: REALLOC_CALLS.load(Ordering::Relaxed),
    };
    assert_eq!(status, OK);
    tree.validate();
    sample
}

fn stats(values: impl Iterator<Item = u128>) -> (u128, u128, u128, u128, u128) {
    let mut values: Vec<u128> = values.collect();
    values.sort_unstable();
    let sum: u128 = values.iter().sum();
    (
        values[0],
        percentile(&values, 50, 100),
        sum / values.len() as u128,
        percentile(&values, 95, 100),
        *values.last().expect("allocation samples"),
    )
}

fn main() {
    let config = Config::parse();

    for _ in 0..config.warmup {
        let tree = BenchTree::new(config.nodes);
        std::hint::black_box(profile(&tree));
    }

    let mut samples = Vec::with_capacity(config.samples);
    for _ in 0..config.samples {
        let tree = BenchTree::new(config.nodes);
        samples.push(profile(&tree));
    }

    let time = stats(samples.iter().map(|sample| sample.elapsed_ns));
    let alloc_calls = stats(samples.iter().map(|sample| sample.alloc_calls as u128));
    let alloc_bytes = stats(samples.iter().map(|sample| sample.alloc_bytes as u128));
    let dealloc_calls = stats(samples.iter().map(|sample| sample.dealloc_calls as u128));
    let dealloc_bytes = stats(samples.iter().map(|sample| sample.dealloc_bytes as u128));
    let realloc_calls = stats(samples.iter().map(|sample| sample.realloc_calls as u128));

    println!(
        "native_allocation_profile nodes={} samples={} time_median={}ns alloc_calls_median={} alloc_bytes_median={} dealloc_calls_median={} dealloc_bytes_median={} realloc_calls_median={}",
        config.nodes,
        config.samples,
        time.1,
        alloc_calls.1,
        alloc_bytes.1,
        dealloc_calls.1,
        dealloc_bytes.1,
        realloc_calls.1
    );
    println!(
        "TAFFY_ALLOCATION_RESULT {{\"nodes\":{},\"samples\":{},\"warmup\":{},\"time_median_ns\":{},\"time_p95_ns\":{},\"alloc_calls_median\":{},\"alloc_calls_p95\":{},\"alloc_bytes_median\":{},\"alloc_bytes_p95\":{},\"dealloc_calls_median\":{},\"dealloc_bytes_median\":{},\"realloc_calls_median\":{}}}",
        config.nodes,
        config.samples,
        config.warmup,
        time.1,
        time.3,
        alloc_calls.1,
        alloc_calls.3,
        alloc_bytes.1,
        alloc_bytes.3,
        dealloc_calls.1,
        dealloc_bytes.1,
        realloc_calls.1
    );
}
