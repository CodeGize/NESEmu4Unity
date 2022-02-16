using NESGame;
using UnityEngine;

public class MainUI : MonoBehaviour
{
    private UnityNes m_nes;

    public GameObject Button;
    // Start is called before the first frame update
    void Start()
    {
        m_nes = FindObjectOfType<UnityNes>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Load()
    {
        m_nes.LoadRom(Application.streamingAssetsPath + "/»ê¶·ÂÞ.zip");
        Button.SetActive(false);
    }
}
