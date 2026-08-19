# Flex Quick Start

This sample demonstrates the runtime result of a simple horizontal Taffy layout.

## Recommended Editor workflow

For normal authoring, create the equivalent layout without writing code:

1. Create or select a Canvas.
2. Choose **GameObject > TaffyUGUI > Horizontal Layout**.
3. Select the Group and keep the Inspector in **Simple** mode.
4. Use **Quick Layout**, alignment, gap, and padding controls to adjust the container.
5. Select children and use Item quick actions such as **Fixed Size**, **Flexible Item**, or **Fit Content**.

Use **Advanced** mode only when you need the complete Flex/Calc/measurement surface.

## Runtime sample

Create an empty UI object with a `RectTransform` under a Canvas, attach `FlexQuickStartSample`, and enter Play Mode. The script creates three visible Taffy Flex items with explicit sizes and spacing.

If the result is unexpected, inspect the Group with **Computed Layout**, **Layout Health**, or **Explain Layout**, or open **Tools > TaffyUGUI > Layout Debugger**.
