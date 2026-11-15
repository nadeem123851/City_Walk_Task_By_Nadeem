using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityWalk
{
    /// <summary>
    /// The light LDR colour grade: a touch more contrast and saturation, warm highlights, cool shadows and a soft
    /// vignette. URP bakes it into one small lookup texture. HDR stays off; the HUD canvases are Screen Space
    /// Overlay, so the grade never touches them.
    /// </summary>
    public static class CityLook
    {
        public static void ApplyPost(Camera cam)
        {
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
        }

        public static void BuildGradeVolume()
        {
            var vol = new GameObject("GradeVolume").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = p.Add<Tonemapping>(true);
            tm.mode.value = TonemappingMode.None;
            var wb = p.Add<WhiteBalance>(true);
            wb.temperature.value = 6f;
            wb.tint.value = 2f;
            var ca = p.Add<ColorAdjustments>(true);
            ca.postExposure.value = 0f;
            ca.contrast.value = 10f;
            ca.saturation.value = 12f;
            var st = p.Add<SplitToning>(true);
            st.shadows.value = CityTextures.Hex(0x6B7A99);
            st.highlights.value = CityTextures.Hex(0x99856B);
            st.balance.value = 0f;
            var vg = p.Add<Vignette>(true);
            vg.intensity.value = 0.20f;
            vg.smoothness.value = 0.45f;
            vg.color.value = CityTextures.Hex(0x0E1A2B);
            vol.sharedProfile = p;
        }
    }
}
