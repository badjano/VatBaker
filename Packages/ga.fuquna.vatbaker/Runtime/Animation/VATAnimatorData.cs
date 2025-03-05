using System.Collections.Generic;
using UnityEngine;

namespace VAT.Animation
{
    [CreateAssetMenu(fileName = "VAT Animator Data", menuName = "VAT/Animation/VAT Animator Data")]
    public class VATAnimatorData : ScriptableObject
    {
        [field: SerializeField] public VATAnimation[] Animations { get; set; }

        private Dictionary<string, VATAnimation> _animations;

        public VATAnimation GetAnimation(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            Dictionary<string, VATAnimation> dict = GetAnimationsDictionary();
            if (dict != null)
            {
                return dict[name];
            }

            return null;
        }

        private Dictionary<string, VATAnimation> GetAnimationsDictionary()
        {
            if (_animations == null || _animations.Count == 0)
            {
                var dict = new Dictionary<string, VATAnimation>();
                foreach (var animation in Animations)
                {
                    dict.Add(animation.Name, animation);
                }

                _animations = dict; // Lazy Initialization
            }

            return _animations;
        }

        private void OnDestroy()
        {
            _animations = null;
        }
    }
}