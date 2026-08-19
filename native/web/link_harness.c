#include "taffy_ugui.h"

#include <math.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

#ifndef TAFFY_EXPECTED_ABI_VERSION
#error "TAFFY_EXPECTED_ABI_VERSION must be supplied by the verification driver"
#endif

#ifndef TAFFY_EXPECTED_ABI_STAGE
#error "TAFFY_EXPECTED_ABI_STAGE must be supplied by the verification driver"
#endif

#define CHECK(condition, message)                    \
    do {                                             \
        if (!(condition)) {                          \
            fprintf(stderr, "FAIL: %s\n", message); \
            return 1;                                \
        }                                            \
    } while (0)
#define TAFFY_CAP_THREAD_LOCAL_CONTEXTS (UINT64_C(1) << 8)


static struct TuValue px(float value) {
    struct TuValue result = {0};
    result.kind = TuValueKind_Length;
    result.value = value;
    return result;
}

static struct TuStyle fixed_style(float width, float height) {
    struct TuStyle style;
    memset(&style, 0, sizeof(style));

    style.display = TuDisplay_Flex;
    style.box_sizing = TuBoxSizing_BorderBox;
    style.direction = TuDirection_Ltr;
    style.overflow_x = TuOverflow_Visible;
    style.overflow_y = TuOverflow_Visible;
    style.position = TuPosition_Relative;
    style.width = px(width);
    style.height = px(height);
    style.margin_left = px(0.0f);
    style.margin_right = px(0.0f);
    style.margin_top = px(0.0f);
    style.margin_bottom = px(0.0f);
    style.flex_direction = TuFlexDirection_Row;
    style.flex_wrap = TuFlexWrap_NoWrap;
    style.flex_shrink = 1.0f;
    style.align_items = TuAlign_Unset;
    style.align_self = TuAlign_Unset;
    style.align_content = TuAlignContent_Unset;
    style.justify_content = TuAlignContent_Unset;
    style.justify_items = TuAlign_Unset;
    style.justify_self = TuAlign_Unset;
    style.float_mode = TuFloatMode_None;
    style.clear_mode = TuClearMode_None;
    style.text_align = TuTextAlign_Auto;
    style.grid_auto_flow = TuGridAutoFlow_Row;
    return style;
}

static int nearly_equal(float actual, float expected) {
    return fabsf(actual - expected) <= 0.01f;
}

static int verify_recoverable_boundary_errors(void) {
    const struct TuStyle style = fixed_style(42.0f, 17.0f);
    TuContextHandle context = 0;
    TuNodeHandle node = 0;
    struct TuLayout layout;
    memset(&layout, 0, sizeof(layout));

    CHECK(tu_context_create(NULL) == TuStatus_NullPointer,
          "null context output did not return NullPointer");
    CHECK(tu_context_destroy(0) == TuStatus_InvalidContext,
          "zero context handle did not return InvalidContext");

    CHECK(tu_context_create(&context) == TuStatus_Ok,
          "boundary-error context create failed");
    CHECK(tu_node_create(context, NULL, &node) == TuStatus_NullPointer,
          "null style pointer did not return NullPointer");
    CHECK(tu_node_create(context, &style, &node) == TuStatus_Ok,
          "boundary-error node create failed");

    struct TuStyle invalid_style = style;
    invalid_style.display = INT32_MAX;
    CHECK(tu_node_set_style(context, node, &invalid_style) == TuStatus_InvalidEnum,
          "invalid display enum did not return InvalidEnum");
    CHECK(tu_compute_layout(context, node, NAN, 480.0f) == TuStatus_InvalidNumber,
          "NaN layout width did not return InvalidNumber");
    CHECK(tu_get_layout(context, 0, &layout) == TuStatus_InvalidNode,
          "zero node handle did not return InvalidNode");

    CHECK(tu_context_destroy(context) == TuStatus_Ok,
          "boundary-error context destroy failed");
    return 0;
}

int main(void) {
    const uint32_t abi_version = tu_get_abi_version();
    const uint32_t abi_stage = tu_get_abi_stage();
    CHECK(abi_version == TAFFY_EXPECTED_ABI_VERSION, "ABI version mismatch");
    CHECK(abi_stage == TAFFY_EXPECTED_ABI_STAGE, "ABI stage mismatch");
    CHECK(verify_recoverable_boundary_errors() == 0,
          "recoverable ABI boundary error checks failed");

    TuContextHandle context = 0;
    CHECK(tu_context_create(&context) == TuStatus_Ok, "context create failed");
    CHECK(context != 0, "context create returned a null handle");
    const uint64_t capabilities = tu_get_capabilities();
    CHECK((capabilities & TAFFY_CAP_THREAD_LOCAL_CONTEXTS) != 0,
          "Web archive does not advertise thread-local contexts");

    const struct TuStyle style = fixed_style(42.0f, 17.0f);
    TuNodeHandle node = 0;
    CHECK(tu_node_create(context, &style, &node) == TuStatus_Ok, "node create failed");
    CHECK(node != 0, "node create returned a null handle");

    CHECK(tu_compute_layout(context, node, 640.0f, 480.0f) == TuStatus_Ok,
          "layout compute failed");

    struct TuLayout layout;
    memset(&layout, 0, sizeof(layout));
    CHECK(tu_get_layout(context, node, &layout) == TuStatus_Ok, "layout retrieval failed");
    CHECK(layout.node == node, "layout returned the wrong node handle");
    CHECK(nearly_equal(layout.width, 42.0f), "layout width mismatch");
    CHECK(nearly_equal(layout.height, 17.0f), "layout height mismatch");

    CHECK(tu_context_clear(context) == TuStatus_Ok, "context clear failed");

    TuNodeHandle post_clear_node = 0;
    CHECK(tu_node_create(context, &style, &post_clear_node) == TuStatus_Ok,
          "node create after context clear failed");
    CHECK(post_clear_node != 0, "post-clear node returned a null handle");

    CHECK(tu_context_destroy(context) == TuStatus_Ok, "context destroy failed");

    printf("TAFFY_WEB_LINK_HARNESS_PASS abi=%u stage=%u width=%.2f height=%.2f\n",
           abi_version,
           abi_stage,
           layout.width,
           layout.height);
    return 0;
}
