using UnityEngine;
#pragma warning disable S3903

[DisallowMultipleComponent]
/**
    @brief Unity Component to track opening and closing windows and popups
 */ 
public class KeewanoWindow : MonoBehaviour
{
    void OnEnable()
    {
        KeewanoSDK.ReportWindowOpen(gameObject.name);
    }

    void OnDisable()
    {
        KeewanoSDK.ReportWindowClose(gameObject.name);
    }
}
