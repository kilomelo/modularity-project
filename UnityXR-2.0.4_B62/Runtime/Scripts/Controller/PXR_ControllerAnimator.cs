/*******************************************************************************
Copyright ? 2015-2022 Pico Technology Co., Ltd.All rights reserved.  

NOTICE£ºAll information contained herein is, and remains the property of 
Pico Technology Co., Ltd. The intellectual and technical concepts 
contained hererin are proprietary to Pico Technology Co., Ltd. and may be 
covered by patents, patents in process, and are protected by trade secret or 
copyright law. Dissemination of this information or reproduction of this 
material is strictly forbidden unless prior written permission is obtained from
Pico Technology Co., Ltd. 
*******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Unity.XR.PXR
{
    public class PXR_ControllerAnimator : MonoBehaviour
    {
        private Animator controllerAnimator;
        public Transform joystick2DAxis;
        public PXR_Input.Controller controller;
        private InputDevice currentController;
        private Vector2 axis2D = Vector2.zero;
        private bool primaryButton;
        private bool secondaryButton;
        private bool menuButton;
        private float grip;
        private float trigger;

        void Start()
        {
            controllerAnimator = GetComponent<Animator>();
            currentController = InputDevices.GetDeviceAtXRNode(controller == PXR_Input.Controller.LeftController ? XRNode.LeftHand : XRNode.RightHand);
        }

        void Update()
        {
            currentController.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis2D);
            currentController.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);
            currentController.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton);
            currentController.TryGetFeatureValue(CommonUsages.menuButton, out menuButton);
            currentController.TryGetFeatureValue(CommonUsages.grip, out grip);
            currentController.TryGetFeatureValue(CommonUsages.trigger, out trigger);
            
            float x = Mathf.Clamp(axis2D.x * 10f, -10f, 10f);
            float z = Mathf.Clamp(axis2D.y * 10f, -10f, 10f);
            if(joystick2DAxis != null)
                joystick2DAxis.localEulerAngles = new Vector3(-z, 0, x);

            if (controllerAnimator != null)
            {
                controllerAnimator.SetFloat("Trigger", trigger);
                controllerAnimator.SetFloat("Grip", grip);
                controllerAnimator.SetFloat("PrimaryButton", primaryButton ? 1.0f : 0.0f);
                controllerAnimator.SetFloat("SecondaryButton", secondaryButton ? 1.0f : 0.0f);
                controllerAnimator.SetFloat("MenuButton", menuButton ? 1.0f : 0.0f);
            }
        }
    }
}

