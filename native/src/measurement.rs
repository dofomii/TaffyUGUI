//! Cached intrinsic measurement input supplied by callers.
//!
//! The native engine never calls back into managed code during layout. Callers upload cached
//! measurements; Taffy consumes them through its normal leaf-measure callback.

use taffy::geometry::Size;
use taffy::style::AvailableSpace;
use crate::error::NativeError;

#[derive(Debug, Clone, Copy, PartialEq)]
pub(crate) struct MeasurementSample { pub available_width: f32, pub size: Size<f32> }

#[derive(Debug, Clone, PartialEq)]
pub(crate) struct MeasurementRecord {
    pub min_content: Size<f32>, pub max_content: Size<f32>, pub preferred: Size<f32>,
    pub aspect_ratio: Option<f32>, pub is_replaced: bool, pub width_samples: Vec<MeasurementSample>,
}
impl Default for MeasurementRecord {
    fn default() -> Self { Self { min_content: Size::ZERO, max_content: Size::ZERO, preferred: Size::ZERO, aspect_ratio: None, is_replaced: false, width_samples: Vec::new() } }
}
impl MeasurementRecord {
    pub(crate) fn validate(&self) -> Result<(), NativeError> {
        let sizes=[self.min_content,self.max_content,self.preferred];
        if sizes.iter().any(|size| !valid_size(*size)) { return Err(NativeError::InvalidValue); }
        if self.aspect_ratio.is_some_and(|ratio| !ratio.is_finite() || ratio <= 0.0) { return Err(NativeError::InvalidValue); }
        if self.width_samples.iter().any(|sample| !sample.available_width.is_finite() || sample.available_width < 0.0 || !valid_size(sample.size)) { return Err(NativeError::InvalidValue); }
        Ok(())
    }
    pub(crate) fn measure(&self, known: Size<Option<f32>>, available: Size<AvailableSpace>) -> Size<f32> {
        if let (Some(width),Some(height))=(known.width,known.height) { return Size{width,height}; }
        let mut measured=self.measure_for_available_width(available.width);
        measured.width=known.width.unwrap_or(measured.width); measured.height=known.height.unwrap_or(measured.height);
        if let Some(ratio)=self.aspect_ratio { match (known.width,known.height) { (Some(width),None) if self.is_replaced => measured.height=width/ratio, (None,Some(height)) if self.is_replaced => measured.width=height*ratio, _=>{} } }
        measured
    }
    fn measure_for_available_width(&self, available: AvailableSpace) -> Size<f32> {
        match available { AvailableSpace::MinContent=>self.min_content, AvailableSpace::MaxContent=>self.max_content,
            AvailableSpace::Definite(width)=> if let Some(sample)=self.closest_width_sample(width){sample.size}else{Size{width:self.preferred.width.min(width.max(0.0)),height:self.preferred.height}} }
    }
    fn closest_width_sample(&self,width:f32)->Option<&MeasurementSample>{self.width_samples.iter().min_by(|left,right|{let l=(left.available_width-width).abs();let r=(right.available_width-width).abs();l.total_cmp(&r)})}
}
fn valid_size(size:Size<f32>)->bool{size.width.is_finite()&&size.height.is_finite()&&size.width>=0.0&&size.height>=0.0}

#[cfg(test)]
mod tests {
    use taffy::style_helpers::TaffyMaxContent; use taffy::{geometry::Size,style::AvailableSpace}; use super::{MeasurementRecord,MeasurementSample};
    #[test] fn known_width_drives_replaced_aspect_ratio(){let record=MeasurementRecord{preferred:Size{width:400.0,height:300.0},aspect_ratio:Some(4.0/3.0),is_replaced:true,..Default::default()};let measured=record.measure(Size{width:Some(200.0),height:None},Size::MAX_CONTENT);assert_eq!(measured,Size{width:200.0,height:150.0});}
    #[test] fn width_dependent_sample_is_selected_without_callback(){let record=MeasurementRecord{preferred:Size{width:300.0,height:20.0},width_samples:vec![MeasurementSample{available_width:100.0,size:Size{width:100.0,height:60.0}},MeasurementSample{available_width:200.0,size:Size{width:200.0,height:40.0}}],..Default::default()};let measured=record.measure(Size::NONE,Size{width:AvailableSpace::Definite(180.0),height:AvailableSpace::MaxContent});assert_eq!(measured,Size{width:200.0,height:40.0});}
}
