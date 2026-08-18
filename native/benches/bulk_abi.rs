mod support;

use std::env;
use std::time::Instant;

use support::{percentile, BenchTree, OK};
use taffy_ugui::*;

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
            samples: 500,
            warmup: 50,
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
                        "usage: cargo bench --bench bulk_abi -- \
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

fn empty_layout() -> TuLayout {
    unsafe { core::mem::zeroed() }
}

fn bulk_sample(tree: &BenchTree, output: &mut [TuLayout]) -> u128 {
    let mut written = 0_u32;
    let start = Instant::now();
    let status = unsafe {
        tu_get_layouts_bulk(
            tree.context(),
            tree.nodes().as_ptr(),
            tree.nodes().len() as u32,
            output.as_mut_ptr(),
            output.len() as u32,
            &mut written,
        )
    };
    let elapsed = start.elapsed().as_nanos();
    assert_eq!(status, OK);
    assert_eq!(written as usize, tree.nodes().len());
    assert!(output.last().expect("bulk layout").width > 0.0);
    elapsed
}

fn scalar_sample(tree: &BenchTree, output: &mut [TuLayout]) -> u128 {
    let start = Instant::now();
    for (index, node) in tree.nodes().iter().copied().enumerate() {
        assert_eq!(
            unsafe { tu_get_layout(tree.context(), node, &mut output[index]) },
            OK
        );
    }
    let elapsed = start.elapsed().as_nanos();
    assert!(output.last().expect("scalar layout").width > 0.0);
    elapsed
}

fn stats(values: &mut [u128]) -> (u128, u128, u128, u128, u128) {
    values.sort_unstable();
    let sum: u128 = values.iter().sum();
    (
        values[0],
        percentile(values, 50, 100),
        sum / values.len() as u128,
        percentile(values, 95, 100),
        *values.last().expect("ABI benchmark samples"),
    )
}

fn main() {
    let config = Config::parse();
    let tree = BenchTree::new(config.nodes);
    assert_eq!(tree.compute(), OK);
    tree.validate();

    let mut bulk_output = vec![empty_layout(); config.nodes];
    let mut scalar_output = vec![empty_layout(); config.nodes];

    for _ in 0..config.warmup {
        std::hint::black_box(bulk_sample(&tree, &mut bulk_output));
        std::hint::black_box(scalar_sample(&tree, &mut scalar_output));
    }

    let mut bulk_timings = Vec::with_capacity(config.samples);
    let mut scalar_timings = Vec::with_capacity(config.samples);
    for _ in 0..config.samples {
        bulk_timings.push(bulk_sample(&tree, &mut bulk_output));
        scalar_timings.push(scalar_sample(&tree, &mut scalar_output));
    }

    let bulk = stats(&mut bulk_timings);
    let scalar = stats(&mut scalar_timings);
    let speedup = scalar.1 as f64 / bulk.1 as f64;

    println!(
        "bulk_abi nodes={} samples={} bulk_median={}ns bulk_p95={}ns scalar_median={}ns scalar_p95={}ns speedup={:.2}x",
        config.nodes, config.samples, bulk.1, bulk.3, scalar.1, scalar.3, speedup
    );
    println!(
        "TAFFY_BULK_ABI_RESULT {{\"nodes\":{},\"samples\":{},\"warmup\":{},\"bulk_min_ns\":{},\"bulk_median_ns\":{},\"bulk_mean_ns\":{},\"bulk_p95_ns\":{},\"bulk_max_ns\":{},\"scalar_min_ns\":{},\"scalar_median_ns\":{},\"scalar_mean_ns\":{},\"scalar_p95_ns\":{},\"scalar_max_ns\":{}}}",
        config.nodes,
        config.samples,
        config.warmup,
        bulk.0,
        bulk.1,
        bulk.2,
        bulk.3,
        bulk.4,
        scalar.0,
        scalar.1,
        scalar.2,
        scalar.3,
        scalar.4
    );
}
