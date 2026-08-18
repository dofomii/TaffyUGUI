mod support;

use std::env;

use support::{percentile, BenchTree};

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
enum Mode {
    First,
    DirtyLeaf,
    Cached,
}

impl Mode {
    fn parse(value: &str) -> Self {
        match value {
            "first" => Self::First,
            "dirty-leaf" => Self::DirtyLeaf,
            "cached" => Self::Cached,
            _ => panic!("unknown benchmark mode: {value}"),
        }
    }

    fn label(self) -> &'static str {
        match self {
            Self::First => "first",
            Self::DirtyLeaf => "dirty-leaf",
            Self::Cached => "cached",
        }
    }
}

#[derive(Debug, Clone, Copy)]
struct Config {
    nodes: usize,
    samples: usize,
    warmup: usize,
    mode: Mode,
}

impl Default for Config {
    fn default() -> Self {
        Self {
            nodes: 100,
            samples: 500,
            warmup: 50,
            mode: Mode::First,
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
                "--mode" => {
                    let mode = args
                        .next()
                        .unwrap_or_else(|| panic!("missing value for {arg}"));
                    config.mode = Mode::parse(&mode);
                }
                "--bench" => {}
                "--help" | "-h" => {
                    println!(
                        "usage: cargo bench --bench layout_scaling -- \
                         [--nodes N] [--samples N] [--warmup N] \
                         [--mode first|dirty-leaf|cached]"
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

fn main() {
    let config = Config::parse();
    let mut timings = Vec::with_capacity(config.samples);

    match config.mode {
        Mode::First => {
            for _ in 0..config.warmup {
                let tree = BenchTree::new(config.nodes);
                std::hint::black_box(tree.compute_ns());
            }
            for _ in 0..config.samples {
                let tree = BenchTree::new(config.nodes);
                timings.push(tree.compute_ns());
            }
        }
        Mode::DirtyLeaf | Mode::Cached => {
            let tree = BenchTree::new(config.nodes);
            assert_eq!(tree.compute(), support::OK);
            tree.validate();

            for _ in 0..config.warmup {
                prepare(&tree, config.mode);
                std::hint::black_box(tree.compute_ns());
            }
            for _ in 0..config.samples {
                prepare(&tree, config.mode);
                timings.push(tree.compute_ns());
            }
        }
    }

    timings.sort_unstable();
    let sum: u128 = timings.iter().sum();
    let mean = sum / timings.len() as u128;
    let min = timings[0];
    let median = percentile(&timings, 50, 100);
    let p95 = percentile(&timings, 95, 100);
    let max = *timings.last().expect("benchmark samples");

    println!(
        "native_layout mode={} nodes={} samples={} warmup={} min={}ns median={}ns mean={}ns p95={}ns max={}ns",
        config.mode.label(),
        config.nodes,
        config.samples,
        config.warmup,
        min,
        median,
        mean,
        p95,
        max
    );
    println!(
        "TAFFY_BENCH_RESULT {{\"benchmark\":\"native_layout\",\"mode\":\"{}\",\"nodes\":{},\"samples\":{},\"warmup\":{},\"min_ns\":{},\"median_ns\":{},\"mean_ns\":{},\"p95_ns\":{},\"max_ns\":{}}}",
        config.mode.label(),
        config.nodes,
        config.samples,
        config.warmup,
        min,
        median,
        mean,
        p95,
        max
    );
}

fn prepare(tree: &BenchTree, mode: Mode) {
    if mode == Mode::DirtyLeaf {
        tree.mark_leaf_dirty();
    }
}
