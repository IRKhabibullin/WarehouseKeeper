using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI debugText;

    void Start()
    {
        
    }

    void Update()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        Vector2 leftStickValue = gamepad.leftStick.ReadValue();
        debugText.text = $"{leftStickValue.x} {leftStickValue.y}";
    }
}
