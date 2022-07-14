using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using imugui.runtime;

[ImuguiWindow(Imugui.EAnchorMode.Center, 0f, 0f, 300, 90)]
public class TestImuWindow : ImuguiBehaviour
{
    private bool _showLabel = true;
    public override void OnImu()
    {
        base.OnImu();
        if (_showLabel) Imu.Label("This is Label");
        Imu.Button("Hello vr imugui", () => {
            Debug.Log("Hello vr imugui btn clicked");
            _showLabel = !_showLabel;
        });
    }
}