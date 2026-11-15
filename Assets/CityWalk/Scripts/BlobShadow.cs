using UnityEngine;
using UnityEngine.Rendering;

namespace CityWalk
{
    /// <summary>
    /// A soft dark oval on the ground under a character. It grounds walkers in building shade, where their real
    /// shadow disappears, and softens the blocky real shadow in the sun.
    /// </summary>
    public static class BlobShadow
    {
        static readonly Color Tint = CityTextures.Hex(0x141C2B, 0.45f);

        public static void Attach(Transform root, float size = 1f)
        {
            if (root == null) return;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BlobShadow";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(root, false);
            quad.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(0.95f * size, 0.75f * size, 1f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = MaterialFactory.GetUnlitTransparentTextured(CityTextures.Blob(), Tint);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }
}
