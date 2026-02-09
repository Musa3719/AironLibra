using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Family
{
    public Humanoid _Father { get; set; }
    public Humanoid _Mother { get; set; }

    private List<Humanoid> _siblings;
    public List<Humanoid> _Siblings { get { if (_siblings == null) return SetSiblings(); else return _siblings; } }
    private List<Humanoid> _children;
    public List<Humanoid> _Children { get { if (_children == null) _children = new List<Humanoid>(); return _children; } }
    private List<Humanoid> SetSiblings()
    {
        _siblings = new List<Humanoid>();
        foreach (var child in _Father._Family._Children)
        {
            _siblings.Add(child);
        }
        foreach (var child in _Mother._Family._Children)
        {
            if (!_siblings.Contains(child))
                _siblings.Add(child);
        }
        return _siblings;
    }

    public void AddChild(Humanoid child)
    {
        _Children.Add(child);
    }

}
