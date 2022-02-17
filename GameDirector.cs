using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameDirector : MonoBehaviour
{
    [SerializeField, Space(5)] 
    GameObject Player = null;
    [SerializeField]
    GameObject Enemies = null;
    [SerializeField] 
    GameObject Weapons = null;
    [SerializeField] 
    GameObject WeaponButtonImages = null;
    [SerializeField]
    Text AmmoText = null;
    [SerializeField]
    Text GrenadeText = null;
    [SerializeField]
    Text SyringeText = null;

    [SerializeField] 
    Image HPBarImage1 = null;
    [SerializeField] 
    Image HPBarImage2 = null;
    [SerializeField] 
    Image HPBarImage3 = null;

    [SerializeField] 
    Image NormalCrossHair = null;
    [SerializeField] 
    Image AimCrossHair = null;
    [SerializeField] 
    Image DamageCrossHair = null;
    [SerializeField] 
    Image KillCrossHair = null;
    [SerializeField] 
    Image DamageDirection = null;
    [SerializeField] 
    Text TimeText = null;
    [SerializeField] 
    Text EnemyNumber = null;

    [SerializeField, Space(5)]
    GameObject VictoryPanel = null;
    [SerializeField]
    GameObject QuitPanel = null;
    [SerializeField]
    GameObject NewTimeText = null;
    [SerializeField]
    GameObject BestTimeText = null;
    [SerializeField]
    GameObject ThisTimeText = null;

    Player player;

    bool getDamageFlag = false;
    bool victoryFlag = false;
    int maxGrenadeNum;
    int maxSyringeNum;

    float time = 0; // 単位s
    int stageEnemyCount;
    float damage_position_x;
    float damage_position_z;

    IEnumerator _DDMTroutine;

    void Start()
    {
        VictoryPanel.SetActive(false);
        player = Player.GetComponent<Player>();
        // グレネード・シリンジのUIを有効化
        maxGrenadeNum = player.MaxGrenadeNum;
        maxSyringeNum = player.MaxSyringeNum;
        GrenadeText.text = maxGrenadeNum.ToString();
        SyringeText.text = maxSyringeNum.ToString();

        // 武器とそのUIを有効化
        for (int i = 0; i < Weapons.transform.childCount; i++)
        {
            Weapons.transform.GetChild(i).gameObject.SetActive(i==0);
            WeaponButtonImages.transform.GetChild(i).gameObject.SetActive(i==0);
        }

        stageEnemyCount = Enemies.transform.childCount;
        InitializeCoroutine();
    }

    void Update()
    {
        ManageHPBar();
        EnemyNumber.text = (stageEnemyCount - Enemies.transform.childCount).ToString() + " / " + stageEnemyCount.ToString();
        if (!victoryFlag) {
            time += Time.deltaTime;
            TimeText.text = ((int)(time / 60)).ToString("00") + ":" + ((int)(time % 60)).ToString("00") + ":" + ((int)((time % 1f) * 100)).ToString("00");
        }
    }

    public void SetWeaponUI(int weaponNum)
    {
        // 武器とそのUIを有効化
        for (int i = 0; i < Weapons.transform.childCount; i++)
        {
            WeaponButtonImages.transform.GetChild(i).gameObject.SetActive(i == weaponNum);
        }

    }
    public void SetBottomUI(int maxGunAmmo, int maxSpareGunAmmo) // GunController.csで使う
    {
        // 弾数（フル）を表示する
        AmmoText.text = maxGunAmmo + " / " + maxSpareGunAmmo;
    }
    
    void ManageHPBar()
    {
        // HPバーの管理
        HPBarImage3.fillAmount = (float)(player.Hp / player.MaxHp);
        if (HPBarImage3.fillAmount == 1)
            HPBarImage2.fillAmount = 1;
        else if (HPBarImage3.fillAmount <= 0.5) 
            HPBarImage1.color = new Color(255, 0, 0, (60f + 60f * Mathf.Sin(1.5f * Time.time)) / 256f);
        else 
            HPBarImage1.color = new Color(0, 0, 0, 150);
        if (HPBarImage3.fillAmount <= HPBarImage2.fillAmount) 
            HPBarImage2.fillAmount -= 1f * Time.deltaTime;
        if (Mathf.Abs(HPBarImage3.fillAmount - HPBarImage2.fillAmount) <= 0.01) 
            HPBarImage2.fillAmount = HPBarImage3.fillAmount;
    }

    public void SetNormalCrossHair(bool aimStatusFlag)
    {
        if (aimStatusFlag)
        {
            // ノーマルクロスヘアを非表示、エイムクロスヘアを表示
            NormalCrossHair.enabled = false; 
            AimCrossHair.enabled = true;
        }
        else
        {
            // ノーマルクロスヘアを表示、エイムクロスヘアを非表示
            NormalCrossHair.enabled = true;
            AimCrossHair.enabled = false;
        }
    }

    public void SetColorNormalCrossHair(int status) // 0: 白, 1: 赤, 2: 青
    {
        if (status == 0)
        {
            NormalCrossHair.color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 180f / 255f); // 白
            AimCrossHair.color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 180f / 255f); // 白
        }
        else
        {
            NormalCrossHair.color = new Color(255f / 255f, 40f / 255f, 40f / 255f, 180f / 255f); // 赤
            AimCrossHair.color = new Color(255f / 255f, 40f / 255f, 40f / 255f, 180f / 255f); // 赤
        }
    }

    public void SetAttackKillCrossHair(int status) // 0: 通常, 1: ダメージ, 2: キル 
    {
        if (status == 0) // 通常時
        {
            DamageCrossHair.enabled = false;
            KillCrossHair.enabled = false;
        }
        else if (status == 1) // ダメージを与えた時
        {
            DamageCrossHair.enabled = true;
            KillCrossHair.enabled = false;
        }
        else // キルした時
        {
            DamageCrossHair.enabled = false;
            KillCrossHair.enabled = true;
        }
    }

    public void SetDamageDirection(float position_x, float position_z)
    {
        damage_position_x = position_x;
        damage_position_z = position_z;
        getDamageFlag = true;
    }

    public void ReturnToMenuSceneButton()
    {
        SceneManager.LoadScene("Menu");
    }

    public float CalcDisToPlayer(float dropItemPos_x, float dropItemPos_y, float dropItemPos_z)
    {
        return Vector3.Distance(Player.transform.position, new Vector3(dropItemPos_x, dropItemPos_y, dropItemPos_z));
    }

    IEnumerator DamageDirectionManageTimer()
    {
        int count = 0;
        DamageDirection.enabled = false;
        while (true)
        {
            yield return new WaitForSeconds(0.01f);
            count++;

            if (getDamageFlag)
            {
                getDamageFlag = false;
                DamageDirection.enabled = true;
                count = 0;
            }

            if(count < 50)
            {
                Vector3 direction1 = new Vector3(damage_position_x - player.transform.position.x, 0f, damage_position_z - player.transform.position.z);
                float directionAngle = Vector3.SignedAngle(player.transform.forward, direction1, player.transform.up);
                if (directionAngle < 0) directionAngle += 360f;
                if (0 <= directionAngle && directionAngle < 45f) directionAngle *= 2f;
                else if (45f <= directionAngle && directionAngle < 315f) directionAngle = (2f / 3f) * directionAngle + 60f;
                else directionAngle = directionAngle * 2f - 360f;
                DamageDirection.transform.rotation = Quaternion.Euler(60, 0, -1f * directionAngle);
            }
            else
            {
                DamageDirection.enabled = false;
            }
        }
    }

    void InitializeCoroutine()
    {
        if(_DDMTroutine == null)
        {
            _DDMTroutine = DamageDirectionManageTimer();
            StartCoroutine(_DDMTroutine);
        }
        else
        {
            StopCoroutine(_DDMTroutine);
            _DDMTroutine = null;
            _DDMTroutine = DamageDirectionManageTimer();
            StartCoroutine(_DDMTroutine);
        }


    }
    public void MenuButtonDown()
    {
        QuitPanel.SetActive(true);
    }

    public void CloseMenuButtonDown()//×とNOのボタンで使用
    {
        QuitPanel.SetActive(false);
    }

    public void SetVictoryPanel()
    {
        VictoryPanel.SetActive(true);
        victoryFlag = true;
        // 新記録の場合
        if (time <= PlayerPrefs.GetFloat("BestTime", 600f))
        {
            NewTimeText.SetActive(true);
            BestTimeText.SetActive(false);
            NewTimeText.GetComponent<Text>().text = "New Record " + ((int)(time / 60)).ToString("00") + ":" + ((int)(time % 60)).ToString("00") + ":" + ((int)((time % 1f) * 100)).ToString("00");
            PlayerPrefs.SetFloat("BestTime", time);
        }
        // ベストタイムに満たない場合
        else
        {
            NewTimeText.SetActive(false);
            BestTimeText.SetActive(true);
            float besttime = PlayerPrefs.GetFloat("BestTime", 600f);
            BestTimeText.GetComponent<Text>().text = "Best Time " + ((int)(besttime / 60)).ToString("00") + ":" + ((int)(besttime % 60)).ToString("00") + ":" + ((int)((besttime % 1f) * 100)).ToString("00");
            ThisTimeText.GetComponent<Text>().text = "This Time " + ((int)(time / 60)).ToString("00") + ":" + ((int)(time % 60)).ToString("00") + ":" + ((int)((time % 1f) * 100)).ToString("00");
        }
    }

    public void TitleButtonDown()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void TryAgainButtonDown()
    {
        SceneManager.LoadScene("GameScene");
    }
}
