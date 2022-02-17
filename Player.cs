using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // インスペクター用変数
    [SerializeField, Range(0, 200)]
    public float MaxHp = 0; // 体力最大値
    [SerializeField, Range(0, 9)]
    public int MaxSyringeNum = 0; // シリンジの最大所持数
    [SerializeField, Range(0, 9)]
    public int MaxGrenadeNum = 0; // グレネードの最大所持数
    [SerializeField, Range(0, 100)]
    float MoveSpeed = 0; // 通常時の移動スピード
    [SerializeField, Range(0, 100)]
    float DashSpeed = 0; // 通常時のダッシュスピード
    [SerializeField, Range(0, 100)]
    float AimMoveSpeed = 0; // エイム時の移動スピード
    [SerializeField, Range(0, 100)]
    float RotateSpeedHor = 0; // カメラの垂直方向の感度
    [SerializeField, Range(0, 100)]
    float RotateSpeedVer = 0; // カメラの水平方向の感度
    [SerializeField, Range(0, 100)]
    float AimRotateSpeedHor = 0; // エイム時のカメラの垂直方向の感度
    [SerializeField, Range(0, 100)]
    float AimRotateSpeedVer = 0; // エイム時のカメラの水平方向の感度
    [SerializeField, Range(0, 1000000)]
    float JumpForce = 0; // ジャンプ時の力


    [SerializeField]
    AudioClip WalkSound = null; // 足音（walk）参照

    [SerializeField,Space(5)] 
    GameObject GameDirector; // ゲームディレクター
    [SerializeField]
    FixedFloatingJoystick JoyStick = null; // ジョイスティック参照
    [SerializeField]
    Camera MainCamera = null; // メインカメラ参照
    [SerializeField]
    GameObject Weapons = null; // メイン武器
    [SerializeField]
    GameObject Grenade = null; // 手榴弾
    [SerializeField]
    GameObject Syringe = null; // シリンジ


    float hp = 0;                 // 体力
    int grenadeNum = 0;
    int syringeNum = 0;
    int preTouchCount = 0;  // タッチしている指の本数
    int swipeFingerID = -1;
    float originalZoom; // 初期状態のカメラのズーム
    Vector2 swipeStartPos;
    Vector2 mouseStartPos;

    // フラグ
    bool moveEnabled = true;
    bool hipShotingStatusFlag = false; // 腰撃ち状態フラグ
    bool aimShotingStatusFlag = false; // エイム撃ち状態フラグ
    bool jumpStatusFlag = false;        // ジャンプ状態フラグ
    bool jumpButtonFlag = false;
    bool mouseStatusFlag = false;       // マウス状態フラグ

    int jsFlag = 1;
    int buttonFlag = 1;
    // HPの自然回復用
    float time_Hp = 0;
    bool flag_Hp = false;

    bool aimFlag = false;
    bool reloadStartedFlag = false;
    bool throwGrenadeFlag = false;
    bool changeWeaponFlag = true; // 初めは初期武器をgetするためtrue

    public int weaponNum; // 現在の武器番号
    int nextWeapon = -1;
    Vector3 StartPosition;
    Vector3 StartAngle;
    Rigidbody rigidBody;
    CapsuleCollider capsuleCollider;
    GameDirector gameDirector;

    AudioSource audioSource;

    public float Hp
    {
        set
        {
            hp = Mathf.Clamp(value, 0, MaxHp);
        }
        get
        {
            return hp;
        }
    }
    public int SyringeNum
    {
        set
        {
            syringeNum = Mathf.Clamp(value, 0, MaxSyringeNum);
        }
        get
        {
            return syringeNum;
        }
    }
    public int GrenadeNum
    {
        set
        {
            grenadeNum = Mathf.Clamp(value, 0, MaxGrenadeNum);
        }
        get
        {
            return grenadeNum;
        }
    }



    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rigidBody = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        gameDirector = GameDirector.GetComponent<GameDirector>();
        weaponNum = 0; // 一番上の武器から
        Hp = MaxHp;
        SyringeNum = MaxSyringeNum;
        GrenadeNum = MaxGrenadeNum;
        StartPosition = transform.position;
        StartAngle = transform.localEulerAngles;
        originalZoom = MainCamera.fieldOfView;
        StartCoroutine(JumpStatusManageTimer());
        StartCoroutine(WalkSoundTimer());
    }

    void Update()
    {
        if (moveEnabled)
        {
            Move();
            Jump();
            SwipeAndMouse();
            AidHP(7);
        }
        if (Input.GetKey(KeyCode.R))
            ChangeToNextWeapon();
    }


    
    // 左のジョイスティック：前後左右の座標移動
    void Move()
    {
        float jh = 0f;
        float jv = 0f;
        // UnityEditerの場合
        if (Application.isEditor)
        {
            if (Input.GetKey(KeyCode.W)) jv = 1f;
            if (Input.GetKey(KeyCode.S)) jv = -1f;
            if (Input.GetKey(KeyCode.A)) jh = -1f;
            if (Input.GetKey(KeyCode.D)) jh = 1f;
        }
        else
        {
            jh = JoyStick.Horizontal;
            jv = JoyStick.Vertical;
        }

        float jn = Mathf.Sqrt(jh * jh + jv * jv);

        // 棒立ち、もしくは、ジャンプ中の場合
        if (jn == 0 || Jump()) jsFlag = 1;
        // 走っている場合
        else if (jv > 0.4f && jn > 0.8f) jsFlag = 3;
        // 歩いている場合
        else jsFlag = 2;

        //-------------
        // XY座標の移動（遷移ではなく状態として考える）
        //-------------
        // IdleもしくはAim_Idleの場合の移動
        if (jn == 0) { }
        // Walk（ジャンプを含む）の場合の移動
        else if ((jsFlag == 2 && !aimFlag) || Jump())
        {
            transform.Translate(MoveSpeed * jh * Time.deltaTime, 0, MoveSpeed * jv * Time.deltaTime);
        }
        // Run（ジャンプを含む）の場合の移動
        else if ((jsFlag == 3 && !aimFlag) || Jump())
        {
            transform.Translate(DashSpeed * jh * Time.deltaTime, 0, DashSpeed * jv * Time.deltaTime);
        }
        // Aim_Walk（ジャンプを含む）の場合の移動
        else if (((jsFlag == 2 || jsFlag == 3) && aimFlag) || Jump())
        {
            transform.Translate(AimMoveSpeed * jh * Time.deltaTime, 0, AimMoveSpeed * jv * Time.deltaTime);
        }

        // ジョイスティック無操作時
        if (jn == 0)
        {
            // 階段で滑る挙動を解決
            rigidBody.velocity = new Vector3(0, rigidBody.velocity.y, 0);
            // ジョイスティック無操作 かつ ジャンプ無操作 時の上昇(吹っ飛び)を解決
            if (!jumpStatusFlag && rigidBody.velocity.y > 0)
                rigidBody.velocity = new Vector3(0, 0, 0);
        }
        
        // ジョイスティック操作に関係なく速さ5以上の場合、速さをリセット(落下は除く)
        if (rigidBody.velocity.magnitude >= 5 && rigidBody.velocity.y >= 0)
        {
            rigidBody.velocity = new Vector3(0, 0, 0);
        }
        
    }
    void SwipeAndMouse()
    {
        // UnityEditerの場合
        if (Application.isEditor) Mouse();
        // スマートフォンの場合
        else Swipe();
    }
    void Swipe()
    {
        // 指の本数が変化した場合、swipeFingerIDを更新
        if (Input.touchCount != preTouchCount)
        {
            preTouchCount = Input.touchCount;
            // swipeFingerIDを特定しなかった場合
            swipeFingerID = -1;
            for (int i = 0; i < Input.touchCount; i++)
            {
                // swipeFingerIDを特定した場合
                if (!EventSystem.current.IsPointerOverGameObject(Input.touches[i].fingerId))
                {
                    swipeFingerID = Input.touches[i].fingerId;
                }
            }
        }

        // swipeFingerの座標を取得
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.touches[i].fingerId == swipeFingerID)
            {
                if (Input.touches[i].phase == TouchPhase.Began)
                {
                    swipeStartPos = Input.touches[i].position;
                }
                else if (Input.touches[i].phase == TouchPhase.Moved)
                {
                    float swipe_x = Mathf.Clamp(Input.touches[i].position.x - swipeStartPos.x, -50, 50);
                    float swipe_y = Mathf.Clamp(Input.touches[i].position.y - swipeStartPos.y, -30, 30);

                    // エイム時の場合
                    if (aimFlag || aimShotingStatusFlag)
                    {
                        transform.Rotate(0, AimRotateSpeedHor * MainCamera.fieldOfView / originalZoom * swipe_x * Time.deltaTime, 0);
                        MainCamera.transform.Rotate(-1 * MainCamera.fieldOfView / originalZoom * AimRotateSpeedVer * swipe_y * Time.deltaTime, 0, 0);
                    }
                    // エイム時でない場合
                    else
                    {
                        transform.Rotate(0, RotateSpeedHor * swipe_x * Time.deltaTime, 0);
                        MainCamera.transform.Rotate(-1 * RotateSpeedVer * swipe_y * Time.deltaTime, 0, 0);
                    }

                    swipeStartPos = Input.touches[i].position;
                }
            }
        }

        // カメラのアングルを制限
        float angle_x;
        if (MainCamera.transform.localEulerAngles.x > 180) angle_x = MainCamera.transform.localEulerAngles.x - 360f;
        else angle_x = MainCamera.transform.localEulerAngles.x;
        if (angle_x > 50) MainCamera.transform.localEulerAngles = new Vector3(50, 0, 0);
        if (angle_x < -60) MainCamera.transform.localEulerAngles = new Vector3(-60, 0, 0);
    }
    void Mouse()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButton(0) && !mouseStatusFlag)
            {
                mouseStartPos = Input.mousePosition;
                mouseStatusFlag = true;
            }
            else if (Input.GetMouseButton(0) && mouseStatusFlag)
            {
                float mouse_x = Input.mousePosition.x - mouseStartPos.x;
                float mouse_y = Input.mousePosition.y - mouseStartPos.y;
                // エイム時の場合
                if (aimFlag || aimShotingStatusFlag)
                {
                    transform.Rotate(0, AimRotateSpeedHor * mouse_x * Time.deltaTime, 0);
                    MainCamera.transform.Rotate(-1 * AimRotateSpeedVer * mouse_y * Time.deltaTime * 0.25f, 0, 0);

                }
                // エイム時でない場合
                else
                {
                    transform.Rotate(0, RotateSpeedHor * mouse_x * Time.deltaTime, 0);
                    MainCamera.transform.Rotate(-1 * RotateSpeedVer * mouse_y * Time.deltaTime * 0.25f, 0, 0);
                }
                mouseStartPos = Input.mousePosition;
                mouseStatusFlag = true;
            }
            else
            {
                mouseStatusFlag = false;
            }

        }
        else
        {
            mouseStatusFlag = false;
        }

        // カメラのアングルを制限
        float angle_x;
        if (MainCamera.transform.localEulerAngles.x > 180) angle_x = MainCamera.transform.localEulerAngles.x - 360f;
        else angle_x = MainCamera.transform.localEulerAngles.x;
        if (angle_x > 50) MainCamera.transform.localEulerAngles = new Vector3(50, 0, 0);
        if (angle_x < -60) MainCamera.transform.localEulerAngles = new Vector3(-60, 0, 0);

    }
    bool Jump()
    {
        // ジャンプボタン押下時、ジャンプ中でない場合
        if (jumpButtonFlag && !jumpStatusFlag)
        {
            // ジャンプ
            rigidBody.AddForce(Vector3.up * JumpForce);
            jumpStatusFlag = true;
        }
        // ジャンプボタンを無効化
        jumpButtonFlag = false;
        return jumpStatusFlag;
    }

    void AidHP(int seconds)
    {
        time_Hp += Time.deltaTime;
        if (time_Hp >= seconds)
        {
            time_Hp = 0;
            flag_Hp = true;
        }
        if (flag_Hp)// フラグがtrueになれば自動回復を始める
        {
            Hp += 50 * Time.deltaTime;
            if (Hp == MaxHp)
                flag_Hp = false;
        }
    }


    // 武器、手榴弾、シリンジをハイドする
    void HideWeapon(int weapon) // 1:武器をハイドする、2:手榴弾をハイドする、3:シリンジをハイドする
    {
        SetChangeWeaponFlag(true);
        if (weapon == 1)
            Weapons.transform.GetChild(weaponNum).gameObject.GetComponent<GunController>().StartHideWeapon();
        else if (weapon == 2)
        {
            Grenade.GetComponent<GrenadeHands>().StartHideGrenade();
        }
        else if(weapon == 3)
        {
            //　不要
        }

    }
    // メイン・サブ武器、手榴弾、シリンジをゲットする
    public void GetWeapon() // グローバル変数nextWeapon...0:メイン武器をゲットする、1:サブ武器をゲットする、2:手榴弾をゲットする、3:シリンジをゲットする
    {
        if (nextWeapon == 1) // 武器をゲットする
        {
            Weapons.transform.GetChild(weaponNum).gameObject.SetActive(true);
            gameDirector.SetWeaponUI(weaponNum);
        }
        else if (nextWeapon == 2) // 手榴弾をゲットする
        {
            Grenade.SetActive(true);
            nextWeapon = 1;
        }
        else if (nextWeapon == 3) // シリンジをゲットする
        {
            Syringe.SetActive(true);
            nextWeapon = 1;
        }
    }

    // 次の武器に切り替え
    public void ChangeToNextWeapon()
    {
        // 最後の武器を所持している場合->クリア
        if (Weapons.transform.childCount <= weaponNum + 1)
        {
            gameDirector.SetVictoryPanel();
            return;
        }

        if (!changeWeaponFlag && Weapons.transform.GetChild(weaponNum).gameObject.activeSelf && !Grenade.activeSelf && !Syringe.activeSelf)
        {
            HideWeapon(1); // 武器をハイドする
            nextWeapon = 1; // 武器をゲットする
            weaponNum++;
        }
    }
    // 手榴弾に切り替え
    public void GetGrenadeButtonDown()
    {
        if (!changeWeaponFlag && GrenadeNum > 0 && !Grenade.activeSelf && !Syringe.activeSelf)
        {
            HideWeapon(1);
            nextWeapon = 2; // 手榴弾をゲットする
        }
    }
    // シリンジに切り替え
    public void GetSyringeButtonDown()
    {
        UnityEngine.Debug.Log(SyringeNum);
        if (!changeWeaponFlag && SyringeNum > 0 && Hp < MaxHp && !Grenade.activeSelf && !Syringe.activeSelf)
        {
            UnityEngine.Debug.Log("b");
            HideWeapon(1);
            nextWeapon = 3; // シリンジをゲットする
        }
    }
    ///---------------------------------------------------------
    /// GunController.cs で使用
    ///---------------------------------------------------------
    public int GetjsFlag()
    {
        return jsFlag;
    }
    public bool GetaimFlag()
    {
        return aimFlag;
    }
    public int GetbuttonFlag()
    {
        return buttonFlag;
    }
    public bool GetreloadStartedFlag()
    {
        bool flag = reloadStartedFlag;
        reloadStartedFlag = false;
        return flag;
    }
    public void CancelAim()
    {
        // Player.cs側のエイム状態を解除
        aimFlag = false;
    }
    public bool GetHipShotingStatusFlag()
    {
        return hipShotingStatusFlag;
    }
    public bool GetAimShotingStatusFlag()
    {
        return aimShotingStatusFlag;
    }
    public bool GetJumpStatusFlag()
    {
        return jumpStatusFlag;
    }

    ///---------------------------------------------------------
    /// Grenade.cs で使用
    ///---------------------------------------------------------
    public bool GetThrowGrenadeFlag()
    {
        return throwGrenadeFlag;
    }

    ///---------------------------------------------------------
    /// 敵キャラのスクリプト で使用
    ///---------------------------------------------------------
    // Playerにダメージを与えた敵のスクリプトで呼び出す
    public bool TakeDamageToPlayer(float damage, float position_x, float position_z)
    {
        if (moveEnabled)
        {
            time_Hp = 0;
            flag_Hp = false;
            Hp -= damage;

            if (Hp <= 0)
            {
                StartCoroutine(DeathTimer());
                return true;
            }
            else
            {
                gameDirector.SetDamageDirection(position_x, position_z);
            }
        }
        return false;

    }

    ///----------------------
    /// UI ボタン
    ///----------------------
    public void HipShotingButtonDown()
    {
        hipShotingStatusFlag = true;
        buttonFlag = 2;
    }
    public void HipShotingButtonUp()
    {
        hipShotingStatusFlag = false;
        buttonFlag = 1;
    }
    public void AimButtonDown()
    {
        if (aimFlag) aimFlag = false;
        else aimFlag = true;
        buttonFlag = 3;
    }
    public void AimButtonUp()
    {
        buttonFlag = 1;
    }
    public void ReloadButtonDown()
    {
        reloadStartedFlag = true;
    }
    public void JumpButtonDown()
    {
        jumpButtonFlag = true;
    }
    public void JumpButtonUp()
    {
        jumpButtonFlag = false;
    }

    IEnumerator DeathTimer()
    {
        moveEnabled = false;
        // 当たり判定を無効化
        capsuleCollider.enabled = false;
        rigidBody.isKinematic = true;
        CancelAim();
        nextWeapon = 1;
        if (Grenade.activeSelf)
        {
            HideWeapon(2);
        }
        else if (Syringe.activeSelf)
        {
            HideWeapon(3);
        }
        yield return new WaitForSeconds(0.5f);
        MainCamera.transform.localEulerAngles = new Vector3(0, 0, 0);
        Hp = MaxHp;
        // 当たり判定を有効化
        capsuleCollider.enabled = true;
        rigidBody.isKinematic = false;
        moveEnabled = true;
        transform.position = StartPosition;
        transform.localEulerAngles = StartAngle; 
    }

    ///---------------------------------------------------------
    /// footスクリプト で使用
    ///---------------------------------------------------------
    public void SetJumpStatusFlag()
    {
        jumpStatusFlag = false;
    }



    IEnumerator JumpStatusManageTimer()
    {
        int count = 0;
        float v = 0f;
        while (true)
        {
            yield return new WaitForSeconds(0.01f);
            if (jumpStatusFlag)
            {
                if (count >= 1 && v == GetComponent<Rigidbody>().velocity.y)
                {
                    count = 0;
                    jumpStatusFlag = false;
                }
                else
                {
                    v = GetComponent<Rigidbody>().velocity.y;
                    count++;
                }
            }
            else
            {
                count = 0;
            }
        }
    }

    IEnumerator WalkSoundTimer()
    {
        
        while (true)
        {
            // Walk
            if((jsFlag == 2 && !aimFlag) && !Jump())
            {
                audioSource.PlayOneShot(WalkSound);
                yield return new WaitForSeconds(0.45f);
            }
            // Run
            else if((jsFlag == 3 && !aimFlag) && !Jump())
            {
                audioSource.PlayOneShot(WalkSound);
                yield return new WaitForSeconds(0.3f);
            }
            // AimWalk
            else if (((jsFlag == 2 || jsFlag == 3) && aimFlag) && !Jump())
            {
                audioSource.PlayOneShot(WalkSound);
                yield return new WaitForSeconds(0.45f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public void SetChangeWeaponFlag(bool Flag)
    {
        changeWeaponFlag = Flag;
    }
}
