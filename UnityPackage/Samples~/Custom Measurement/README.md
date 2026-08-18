# Custom Measurement

Create a Taffy Flex/Grid container, add a child UI object, and attach `CustomMeasurementSample`. The component implements `ITaffyMeasurementProvider`; Taffy resolves its preferred size before the native compute pass. Change the public dimensions in Play Mode and invalidate measurement when your own provider data changes.
