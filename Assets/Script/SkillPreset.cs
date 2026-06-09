using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSkillPreset", menuName = "Scriptable Objects/SkillPreset")]
public class SkillPreset : ScriptableObject
{
    public string presetName;
    public List<SkillData> skills = new List<SkillData>();
}
