//! Web-only allocator bridge for generic wasm32 archives linked by Unity/Emscripten.
//!
//! Older Rust `wasm32-unknown-unknown` standard libraries use a standalone allocator that
//! does not interoperate with Unity's Emscripten-linked linear-memory layout. The Web archive
//! therefore delegates allocation to the final Emscripten runtime while remaining independent
//! of Emscripten headers and libraries at Rust build time.

use std::alloc::{GlobalAlloc, Layout};
use std::ffi::c_void;
use std::ptr::{copy_nonoverlapping, null_mut};

unsafe extern "C" {
    fn malloc(size: usize) -> *mut c_void;
    fn free(ptr: *mut c_void);
}

struct EmscriptenAllocator;

unsafe impl GlobalAlloc for EmscriptenAllocator {
    unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
        let align = layout.align();
        let size = layout.size().max(1);
        let header = std::mem::size_of::<usize>();
        let Some(total) = size
            .checked_add(align - 1)
            .and_then(|value| value.checked_add(header))
        else {
            return null_mut();
        };

        let base = unsafe { malloc(total) }.cast::<u8>();
        if base.is_null() {
            return null_mut();
        }

        let start = unsafe { base.add(header) } as usize;
        let aligned = (start + align - 1) & !(align - 1);
        let pointer = aligned as *mut u8;
        unsafe { pointer.sub(header).cast::<usize>().write(base as usize) };
        pointer
    }

    unsafe fn dealloc(&self, pointer: *mut u8, _layout: Layout) {
        if pointer.is_null() {
            return;
        }
        let header = std::mem::size_of::<usize>();
        let base = unsafe { pointer.sub(header).cast::<usize>().read() } as *mut c_void;
        unsafe { free(base) };
    }

    unsafe fn realloc(&self, pointer: *mut u8, layout: Layout, new_size: usize) -> *mut u8 {
        let new_layout =
            unsafe { Layout::from_size_align_unchecked(new_size.max(1), layout.align()) };
        let new_pointer = unsafe { self.alloc(new_layout) };
        if new_pointer.is_null() {
            return null_mut();
        }

        unsafe {
            copy_nonoverlapping(pointer, new_pointer, std::cmp::min(layout.size(), new_size));
            self.dealloc(pointer, layout);
        }
        new_pointer
    }
}

#[global_allocator]
static WEB_ALLOCATOR: EmscriptenAllocator = EmscriptenAllocator;
