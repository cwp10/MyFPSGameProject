using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    private static GameManager instance = null;
    public static GameManager Instance { get { return instance; } }

    public Image takeDamageImage;

    void Awake() {
        if (instance == null)
        {
            instance = this;
        }
    }
}
