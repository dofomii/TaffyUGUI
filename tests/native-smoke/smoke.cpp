#include "taffy_ugui.h"
#include <cstdint>
#include <type_traits>

static_assert(sizeof(TuContextHandle) == 8);
static_assert(sizeof(TuNodeHandle) == 8);
static_assert(std::is_standard_layout<TuStyle>::value);
static_assert(std::is_standard_layout<TuLayout>::value);
static_assert(TuAlign_Unset == -1);
static_assert(TuGridPlacementKind_NamedSpan == 4);

int main() {
    const auto caps = tu_get_capabilities();
    const auto version = tu_get_taffy_version_packed();
    return (caps != 0 && version == (13u << 12)) ? 0 : 1;
}
