using System;
using UnityEngine;

namespace CityWalk
{
    /// <summary>
    /// Receives the "Footstep" animation events placed on the walk and run clips at each foot contact.
    /// Sits next to the Animator. The pedestrians share the clips, so they carry one too (with nobody listening),
    /// which keeps Unity from warning about events without a receiver.
    /// </summary>
    public class FootstepEvents : MonoBehaviour
    {
        /// <summary>Raised on each footstep of the dominant clip in the blend.</summary>
        public event Action Stepped;

        // Called by the animation event of that name.
        void Footstep(AnimationEvent e)
        {
            // In a blend tree every playing clip fires its events; ignore the faint ones (listeners also
            // skip a step that lands right after another, for when two clips are blended half and half).
            if (e.animatorClipInfo.weight < 0.25f) return;
            Stepped?.Invoke();
        }
    }
}
