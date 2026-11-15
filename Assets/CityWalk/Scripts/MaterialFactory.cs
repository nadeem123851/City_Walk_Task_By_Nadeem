using System.Collections.Generic;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Creates simple coloured materials.
    /// Copies the URP Lit material assets in Resources so their shader (and its emission variant)
    /// is included in player builds. In a build, a primitive's default material uses the Built-in
    /// shader, which URP draws pink. Falls back to that default material when the assets are missing,
    /// so the Built-in pipeline still works.
    /// </summary>
    public static class MaterialFactory
    {
        const string BaseLitPath = "CityWalk_BaseLit";
        const string BaseLitEmissivePath = "CityWalk_BaseLitEmissive";
        const string BaseUnlitPath = "CityWalk_BaseUnlit";
        const string UnlitTransparentPath = "CityWalk_UnlitTransparent";

        static Material baseMaterial;
        static Material emissiveBaseMaterial;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        static Material BaseMaterial
        {
            get
            {
                if (baseMaterial == null)
                {
                    baseMaterial = Resources.Load<Material>(BaseLitPath);
                    if (baseMaterial == null)
                    {
                        var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        baseMaterial = probe.GetComponent<Renderer>().sharedMaterial;
                        Object.DestroyImmediate(probe);
                    }
                }
                return baseMaterial;
            }
        }

        static Material EmissiveBaseMaterial
        {
            get
            {
                if (emissiveBaseMaterial == null)
                {
                    emissiveBaseMaterial = Resources.Load<Material>(BaseLitEmissivePath);
                    if (emissiveBaseMaterial == null) emissiveBaseMaterial = BaseMaterial;
                }
                return emissiveBaseMaterial;
            }
        }

        public static Material Get(Color color, float emission = 0f)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + "_" + emission.ToString("0.00");
            if (cache.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var source = emission > 0f ? EmissiveBaseMaterial : BaseMaterial;
            var mat = new Material(source) { name = "CityWalk_" + key };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.15f);

            if (emission > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", color * emission);
            }

            cache[key] = mat;
            return mat;
        }

        /// <summary>A lit material tinted by color with a texture, for example building windows.</summary>
        public static Material GetTextured(Color color, Texture texture)
        {
            if (texture == null) return Get(color);
            string key = "tex_" + texture.name + "_" + ColorUtility.ToHtmlStringRGBA(color);
            if (cache.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var mat = Get(color);
            mat = new Material(mat) { name = "CityWalk_" + key };
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            cache[key] = mat;
            return mat;
        }

        /// <summary>As <see cref="GetTextured(Color, Texture)"/>, with the texture repeated <paramref name="tiling"/> times.</summary>
        public static Material GetTextured(Color color, Texture texture, Vector2 tiling)
        {
            if (texture == null) return Get(color);
            string key = $"tex_{texture.name}_{ColorUtility.ToHtmlStringRGBA(color)}_{tiling.x:0.##}x{tiling.y:0.##}";
            if (cache.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var mat = new Material(GetTextured(color, texture)) { name = "CityWalk_" + key };
            if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling);
            if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", tiling);
            cache[key] = mat;
            return mat;
        }

        /// <summary>An unlit, alpha-blended textured material drawn from both sides, for soft ground shading.</summary>
        public static Material GetUnlitTransparentTextured(Texture texture, Color tint)
        {
            string key = "unlitTT_" + (texture != null ? texture.name : "none") + "_" + ColorUtility.ToHtmlStringRGBA(tint);
            if (cache.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var mat = new Material(GetUnlitColor(tint, true)) { name = "CityWalk_" + key };
            if (texture != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            }
            cache[key] = mat;
            return mat;
        }

        /// <summary>Turns off shadow casting on a primitive (flat ground pieces cast nothing useful).</summary>
        public static GameObject NoShadow(this GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>An unlit textured material, for signs that should read clearly in shade.</summary>
        public static Material GetUnlit(Texture texture)
        {
            string key = "unlit_" + (texture != null ? texture.name : "none");
            if (cache.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var source = Resources.Load<Material>(BaseUnlitPath);
            Material mat = source != null ? new Material(source) : new Material(BaseMaterial);
            mat.name = "CityWalk_" + key;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (texture != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            }
            cache[key] = mat;
            return mat;
        }

        /// <summary>
        /// A flat-colour unlit material drawn from both sides, for guidance visuals.
        /// Transparent ones blend by the colour's alpha and don't write depth.
        /// </summary>
        public static Material GetUnlitColor(Color color, bool transparent)
        {
            string key = (transparent ? "unlitT_" : "unlitO_") + ColorUtility.ToHtmlStringRGBA(color);
            if (cache.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var source = Resources.Load<Material>(transparent ? UnlitTransparentPath : BaseUnlitPath);
            Material mat = source != null ? new Material(source) : new Material(BaseMaterial);
            mat.name = "CityWalk_" + key;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);   // both sides
            cache[key] = mat;
            return mat;
        }

        /// <summary>Creates a coloured primitive. Colliders are removed unless keepCollider is true.</summary>
        public static GameObject Primitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Color color, bool keepCollider = false, float emission = 0f)
        {
            return Primitive(type, name, parent, localPosition, localScale, Get(color, emission), keepCollider);
        }

        /// <summary>Creates a primitive with the given material. Colliders are removed unless keepCollider is true.</summary>
        public static GameObject Primitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            return go;
        }
    }
}
