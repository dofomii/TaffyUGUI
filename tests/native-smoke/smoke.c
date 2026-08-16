#include "taffy_ugui.h"
#include <math.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#if defined(__STDC_VERSION__) && __STDC_VERSION__ >= 201112L
_Static_assert(sizeof(TuContextHandle) == 8, "context handles must be uint64");
_Static_assert(sizeof(TuNodeHandle) == 8, "node handles must be uint64");
_Static_assert(sizeof(TuResourceHandle) == 8, "resource handles must be uint64");
_Static_assert(sizeof(TuStatus) == 4, "status enum must be int32-sized on supported toolchains");
_Static_assert(TuStatus_Ok == 0, "status numeric contract changed");
_Static_assert(TuStatus_InternalPanic == -13, "status numeric contract changed");
_Static_assert(TuDisplay_FlowRoot == 4, "display numeric contract changed");
_Static_assert(TuGridTrackKind_Repeat == 8, "grid track numeric contract changed");
#endif
static TuStyle base_style(void){TuStyle s;memset(&s,0,sizeof(s));s.display=TuDisplay_Flex;s.box_sizing=TuBoxSizing_BorderBox;s.direction=TuDirection_Ltr;s.overflow_x=TuOverflow_Visible;s.overflow_y=TuOverflow_Visible;s.position=TuPosition_Relative;s.flex_direction=TuFlexDirection_Row;s.flex_wrap=TuFlexWrap_NoWrap;s.flex_shrink=1.0f;s.align_items=TuAlign_Unset;s.align_self=TuAlign_Unset;s.align_content=TuAlignContent_Unset;s.justify_content=TuAlignContent_Unset;s.justify_items=TuAlign_Unset;s.justify_self=TuAlign_Unset;s.float_mode=TuFloatMode_None;s.clear_mode=TuClearMode_None;s.text_align=TuTextAlign_Auto;s.grid_auto_flow=TuGridAutoFlow_Row;s.margin_left.kind=TuValueKind_Length;s.margin_right.kind=TuValueKind_Length;s.margin_top.kind=TuValueKind_Length;s.margin_bottom.kind=TuValueKind_Length;return s;}
int main(void){if(tu_get_abi_version()!=1u)return 10;if(tu_get_abi_stage()!=1u)return 11;if(tu_get_taffy_version_packed()!=(13u<<12))return 12;TuContextHandle context=0;if(tu_context_create(&context)!=TuStatus_Ok||context==0)return 13;TuStyle root_style=base_style();root_style.width.kind=TuValueKind_Length;root_style.width.value=100.0f;root_style.height.kind=TuValueKind_Length;root_style.height.value=40.0f;TuNodeHandle root=0;if(tu_node_create(context,&root_style,&root)!=TuStatus_Ok||root==0)return 14;if(tu_compute_layout(context,root,100.0f,40.0f)!=TuStatus_Ok)return 15;TuLayout layout;memset(&layout,0,sizeof(layout));if(tu_get_layout(context,root,&layout)!=TuStatus_Ok)return 16;if(fabsf(layout.width-100.0f)>0.01f||fabsf(layout.height-40.0f)>0.01f)return 17;if(tu_context_destroy(context)!=TuStatus_Ok)return 18;return 0;}
