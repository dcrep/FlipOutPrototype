using UnityEngine;

[CreateAssetMenu(fileName = "AudioSources", menuName = "Scriptable Objects/AudioSources")]
public class AudioSourcesSO : ScriptableObject
{
    public AudioClip[] musicClips;
    public AudioClip UIMenuClick;
    public AudioClip clickCard;
}
