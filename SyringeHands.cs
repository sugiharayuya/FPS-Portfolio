using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SyringeHands : MonoBehaviour
{
    public GameObject Player; // プレイヤー参照
    [SerializeField]
    Text SyringeText = null; // シリンジテキスト参照
    public float GetInterval; // 
    public float UseInterval; // 
    public float HideInterval; // 

    Player player;

    bool startFlag = true;
    void OnEnable()
    {
        if (startFlag)
        {
            startFlag = false;
            // Start()でさせたい処理
            player = Player.GetComponent<Player>();
        }
        // OnEnable()でさせたい処理
        StartCoroutine(Timer());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator Timer()
    {
        yield return new WaitForSeconds(GetInterval);
        yield return new WaitForSeconds(UseInterval * 2f / 3f);
        player.Hp = 1000;
        yield return new WaitForSeconds(UseInterval / 3f);
        player.SyringeNum--;
        SyringeText.text = player.SyringeNum.ToString();
        yield return new WaitForSeconds(HideInterval);
        player.GetWeapon();
        this.gameObject.SetActive(false);
    }
}
