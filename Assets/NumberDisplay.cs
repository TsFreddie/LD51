using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberDisplay : MonoBehaviour
{
    public Sprite[] Sprites;
    public SpriteRenderer Renderer;

    private int _number = 0;
    public int Number
    {
        get => _number;
        set
        {
            _number = value;
            if (_number < 0 || _number >= Sprites.Length)
            {
                Renderer.enabled = false;
            }
            else
            {
                Renderer.enabled = true;
                Renderer.sprite = Sprites[_number];
            }
        }
    }
}
