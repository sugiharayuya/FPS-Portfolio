using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GunController : MonoBehaviour
{
    public bool ShotEnabled; // 射撃可能フラグ true
    public bool SRFlag; // スナイパーの場合true
    public bool ScarFlag; // Scarの場合true
    public bool M4Flag; // M4の場合true
    public bool MP5Flag; // MP5の場合true
    public bool HGFlag; // HGの場合true
    public bool RVFlag; // RVの場合true
    public bool SGFlag; // ショットガンの場合true
    public bool CBFlag; // クロスボウの場合true
    public bool GLFlag; // グレネードランチャーの場合true
    public int MaxSpareAmmo; // 最大弾薬数
    public int MaxGunAmmo; // 弾倉の最大弾薬数
    public int Damage; // 弾丸のダメージ
    public float ShotRange; // 射程距離
    public float AimZoom; // エイム時のズーム具合
    public float ZoomInSpeed; // ズームインのスピード
    public float ZoomOutSpeed; // ズームアウトのスピード
    public float GetInterval; // 武器を出すまでに掛かる時間
    public float ShotInterval; // 射撃に掛かる時間
    public float ReloadInterval; // リロードに掛かる時間
    public float HideInterval; // 武器を隠すまでに掛かる時間

    public GameObject GameDirector; // ゲームディレクター
    public GameObject Trajectory; // 弾道の原点
    public GameObject Muzzle; // マズルフラッシュの原点
    public GameObject MuzzleFlashPrefab; // マズルフラッシュのプレファブ
    public GameObject BulletHolePrefab; // バレットホールのプレファブ
    public GameObject HitEffectPrefab; // ヒットエフェクトのプレファブ
    public GameObject Player; // プレイヤー参照
    public GameObject CBArrow; // クロスボウの矢
    public GameObject ShotGunSleeve; // ショットガンの弾
    public GameObject GrenadeForLauncherPrefab; // グレネードランチャーのグレネード
    public Text AmmoText; // 弾薬テキスト参照
    public Camera MainCamera;　// メインカメラ参照
    public GameObject MainCameraObject;　// メインカメラ参照
    public GameObject SniperCamera; // スナイパー用カメラ
    public GameObject SniperAimImage; // スナイパーエイム画像
    public float SniperCameraUpDistance;
    public float SniperCameraUpSpeed;
    public float SniperCameraDownSpeed;
    public AudioClip ShotSound; // 射撃音参照
    public AudioClip ReloadSound; // リロード音参照
    public AudioClip ReloadEndSound; // リロード終了音（ショットガン）参照
    public AudioClip WeaponSelectSound; // 武器選択音参照
    bool shoting = false; // 射撃中フラグ
    bool reloading = false; // リロード終了時のみに立ち上がるフラグ
    bool hiding = false;
    bool getting= false;
    bool hideFinishedFlag = false; // Player.csに渡す

    bool attackShotFlag = false;
    bool killShotFlag = false;
    int spareAmmo; // 弾薬数
    int gunAmmo; // 弾倉の弾薬数
    int SniperCameraStatus = 0;
    float originalZoom; // 初期状態のカメラのズーム
    float sumTime = 0f;

    Vector3 SniperCameraNormalPos;
    Vector3 SniperCameraUpPos;
    Animator animator;
    AudioSource audioSource;
    GameDirector gameDirector;
    Player player;
    Camera mainCamera_Camera = null;
    GameObject bulletHole = null;
    GameObject hitEffect = null;

    Coroutine _someCoroutine; // ゲットコルーチンとリロードコルーチン取得用
    Coroutine _crossHairManageCoroutine; // クロスヘア管理コルーチン取得用



    bool startHideWeaponFlag = false;


    bool startFlag = true;
    void OnEnable()
    {

        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        gameDirector = GameDirector.GetComponent<GameDirector>();
        player = Player.GetComponent<Player>();
        mainCamera_Camera = MainCamera.GetComponent<Camera>();
        originalZoom = MainCamera.fieldOfView;
        if (AimZoom == 0) AimZoom = originalZoom;
        if (SRFlag)
        {
            SniperCameraNormalPos = SniperCamera.transform.localPosition;
            SniperCameraUpPos = SniperCameraNormalPos + new Vector3(0, SniperCameraUpDistance, 0);
        }

        reloading = false;
        shoting = false;
        hiding = false;
        player.GetComponent<Player>().CancelAim();
        _someCoroutine = StartCoroutine(GetTimer());
        _crossHairManageCoroutine = StartCoroutine(CrossHairManageTimer());

        InitializeAmmo();
        gameDirector.SetBottomUI(MaxGunAmmo, MaxSpareAmmo);
        if (SGFlag) ShotGunSleeve.SetActive(false);
    }

    void OnDisable()
    {
        StopCoroutine(_someCoroutine);
        StopCoroutine(_crossHairManageCoroutine);
        animator.SetInteger("Status", 0);
    }


    void Update()
    {

        int jsFlag = player.GetjsFlag();
        bool aimFlag = player.GetaimFlag();
        int buttonFlag = player.GetbuttonFlag();
        bool reloadStartedFlag = player.GetreloadStartedFlag();

        // リロード中でも射撃中でも武器隠し中でも武器取得中でもない場合
        if (!reloading && !shoting && !hiding && !getting) {
            if (reloadStartedFlag)
            {
                //Recharge
                if (spareAmmo > 0 && gunAmmo < MaxGunAmmo)
                {
                    gameDirector.SetNormalCrossHair(false);
                    MainCamera.fieldOfView = originalZoom; //ズームアウト(瞬時)
                    ScopeCameraSet(false);
                    _someCoroutine = StartCoroutine(ReloadTimer());
                }
            }
            else if (aimFlag && buttonFlag == 2)
            {
                //Aim_Shot
                if(gunAmmo > 0)
                {
                    gameDirector.SetNormalCrossHair(true);
                    MainCamera.fieldOfView = AimZoom; //ズームイン(瞬時)
                    ScopeCameraSet(true);
                    StartCoroutine(ShotTimer(true));
                }
                //X Aim_Shot -> Recharge
                else
                {
                    //ズームアウト(瞬時)
                    MainCamera.fieldOfView = originalZoom;
                    ScopeCameraSet(false);
                    if (gunAmmo < MaxGunAmmo && spareAmmo > 0)
                    {
                        gameDirector.SetNormalCrossHair(false);
                        _someCoroutine = StartCoroutine(ReloadTimer());
                    }
                }

            }
            else if (!aimFlag && buttonFlag == 2)
            {
                //ズームアウト(瞬時)
                MainCamera.fieldOfView = originalZoom;
                ScopeCameraSet(false);
                //Shot
                if (gunAmmo > 0)
                {
                    gameDirector.SetNormalCrossHair(false);
                    StartCoroutine(ShotTimer(false));
                }
                //X Shot -> Recharge
                else
                {
                    if(gunAmmo < MaxGunAmmo && spareAmmo > 0)
                    {
                        gameDirector.SetNormalCrossHair(false);
                        _someCoroutine = StartCoroutine(ReloadTimer());
                    }
                }
            }
            else if ((!aimFlag && buttonFlag == 1) || (aimFlag && buttonFlag == 3))
            {
                //ズームアウト(瞬時)
                CameraAimZoomSet(false);
                ScopeCameraSet(false);
                if (jsFlag == 1)
                {
                    //Idle
                    gameDirector.SetNormalCrossHair(false);
                    animator.SetInteger("Status", 0);
                }
                else if (jsFlag == 2)
                {
                    //Walk
                    gameDirector.SetNormalCrossHair(false);
                    animator.SetInteger("Status", 1);
                }
                else
                {
                    //Run
                    gameDirector.SetNormalCrossHair(false);
                    animator.SetInteger("Status", 2);
                }
            }
            else
            {
                //ズームイン(滑らか)
                if (CameraAimZoomSet(true))
                    // ズームイン完了時にスコープカメラセット
                    ScopeCameraSet(true);
                if (jsFlag == 1)
                {
                    gameDirector.SetNormalCrossHair(true);
                    //Aim_Idle
                    animator.SetInteger("Status", 4);
                }
                else
                {
                    gameDirector.SetNormalCrossHair(true);
                    //Aim_Walk
                    animator.SetInteger("Status", 5);
                }
            }
        }

        //Hide gettingが有効の場合は終わるまで待つ
        if (startHideWeaponFlag && !getting && !hiding)
        {
            startHideWeaponFlag = false;
            gameDirector.SetNormalCrossHair(false);
            ScopeCameraSet(false);
            StartCoroutine(HideTimer());
        }

        if (hiding)
        {
            //ズームアウト(滑らか)
            CameraAimZoomSet(false);
        }


        if (SniperCameraStatus == 1) // 跳ね上がり
        {
            sumTime += Time.deltaTime;
            SniperCamera.transform.localPosition = Vector3.Lerp(SniperCameraNormalPos, SniperCameraUpPos, (sumTime * SniperCameraUpSpeed) / SniperCameraUpDistance);
            if ((sumTime * SniperCameraUpSpeed) / SniperCameraUpDistance >= 1)
            {
                SniperCameraStatus = 2;
                sumTime = 0f;
            }
        }
        else if (SniperCameraStatus == 2)// 戻り
        {
            sumTime += Time.deltaTime;
            SniperCamera.transform.localPosition = Vector3.Lerp(SniperCameraUpPos, SniperCameraNormalPos, (sumTime * SniperCameraDownSpeed) / SniperCameraUpDistance);
            if ((sumTime * SniperCameraDownSpeed) / SniperCameraUpDistance >= 1)
            {
                SniperCameraStatus = 0;
                sumTime = 0f;
            }
        }
        else { } // 通常時 SniperCameraStatus = 0
    }

    IEnumerator GetTimer()
    {
        getting = true;
        if(player.transform.position.y > 0)
            audioSource.PlayOneShot(WeaponSelectSound);
        yield return new WaitForSeconds(GetInterval + 0.25f);
        player.SetChangeWeaponFlag(false);
        getting = false;
    }
    IEnumerator ShotTimer(bool AimStatus) // true -> aim_shot , false -> shot
    {
        shoting = true;
        if (AimStatus) // エイム射撃
            animator.SetInteger("Status", 6);
        else // 腰射撃
            animator.SetInteger("Status", 3);

        audioSource.PlayOneShot(ShotSound);
        // グレネードランチャーの場合
        if (GLFlag)
        {
            yield return new WaitForSeconds(ShotInterval * 3.0f / 8.0f);
            gunAmmo--;
            AmmoText.text = gunAmmo + " / " + spareAmmo;
            // ここでランチャーのグレネードを飛ばす
            GameObject grenadeForLauncher = Instantiate(GrenadeForLauncherPrefab, Muzzle.transform.position, Quaternion.Euler(90, 0, 0));
            grenadeForLauncher.GetComponent<Rigidbody>().AddForce(Muzzle.transform.forward * 5000);
            yield return new WaitForSeconds(ShotInterval * 5.0f / 8.0f);
        }
        // グレネードランチャー以外の場合
        else
        {
            transform.localEulerAngles += new Vector3(-0.5f, 0, 0);// 垂直反動（見た目のみ）
            if (SniperCamera != null)
                SniperCameraStatus = 1;
            Ray ray;
            RaycastHit hit;
            if (AimStatus) // エイム射撃
            {
                ray = new Ray(Trajectory.transform.position, Trajectory.transform.forward);
            }
            else // 腰射撃
            {
                Vector3 hipRaydir = Trajectory.transform.forward + new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(-0.08f, 0.08f), 0);
                ray = new Ray(Trajectory.transform.position, hipRaydir);
            }
            // マズルフラッシュの生成
            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlash = Instantiate(MuzzleFlashPrefab, Muzzle.transform.position, Muzzle.transform.rotation, Muzzle.transform);
                muzzleFlash.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            }

            if (SRFlag)
            {
                yield return new WaitForSeconds(ShotInterval / 10f);
            }
            if (ScarFlag || M4Flag || MP5Flag || RVFlag)
                for (int i = 0; i < 4; i++) // 0.02s
                    yield return new WaitForFixedUpdate();
            else if (SGFlag)
                for (int i = 0; i < 40; i++) // 0.20s
                    yield return new WaitForFixedUpdate();
            else { }


            // 弾薬処理
            gunAmmo--;
            AmmoText.text = gunAmmo + " / " + spareAmmo;

            // クロスボウの矢非表示
            if (CBFlag) 
                CBArrow.SetActive(false);

            // ダメージ処理
            if (Physics.Raycast(ray, out hit, ShotRange) && hit.collider != null)
            {
                if (hit.collider.gameObject.tag == "Enemy")
                {
                    // キルした時
                    if (hit.collider.gameObject.GetComponent<Character>().TakeDamageToTarget(Damage))
                    {
                        killShotFlag = true;
                        player.ChangeToNextWeapon();
                    }
                    // ダメージを与えた時
                    else
                    {
                        attackShotFlag = true;
                    }
                    // ヒットエフェクトの生成
                    if (HitEffectPrefab != null)
                    {
                        if (hitEffect == null)
                            hitEffect = Instantiate(HitEffectPrefab, hit.point, Quaternion.identity);
                        else
                        {
                            hitEffect.SetActive(false);
                            hitEffect.transform.position = hit.point;
                            hitEffect.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                            hitEffect.SetActive(true);
                        }
                    }
                }
                else
                {
                    // バレットホールの生成
                    if (BulletHolePrefab != null)
                    {
                        bulletHole = Instantiate(BulletHolePrefab, hit.point, Quaternion.identity);
                        bulletHole.transform.position = hit.point;
                        bulletHole.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                    }
                }
            }
            if (SRFlag)
                yield return new WaitForSeconds(ShotInterval * 9f / 10f);
            else if (ScarFlag || M4Flag || MP5Flag)
                for (int i = 0; i < 4; i++) // 0.02s
                    yield return new WaitForFixedUpdate();
            else if (SGFlag)
                for (int i = 0; i < 124; i++) // 0.20s
                    yield return new WaitForFixedUpdate();
            else if (HGFlag)
                for (int i = 0; i < 50; i++) // 0.25s
                    yield return new WaitForFixedUpdate();
            else if (RVFlag)
                for (int i = 0; i < 96; i++) // 0.48s
                    yield return new WaitForFixedUpdate();

            transform.localEulerAngles += new Vector3(0.5f, 0, 0);// 垂直反動（見た目のみ）
        }

        if (AimStatus)
            animator.SetInteger("Status", 4);
        else
            animator.SetInteger("Status", 0);

        for (int i = 0; i < 8; i++) // 0.04s
            yield return new WaitForFixedUpdate();
        shoting = false;


    }

    IEnumerator ReloadTimer()
    {
        player.GetComponent<Player>().CancelAim();
        reloading = true;
        int reloadAmmo = Mathf.Clamp(MaxGunAmmo - gunAmmo, 0, spareAmmo);
        // クロスボウの場合
        if (CBFlag)
        {
            CBArrow.SetActive(true);
        }
        if (SGFlag)
        {
            animator.SetInteger("Status", reloadAmmo + 6); 
            yield return new WaitForSeconds(0.64f);
            for (int i=0; i < reloadAmmo; i++)
            {
                yield return new WaitForSeconds(0.15f);
                audioSource.PlayOneShot(ReloadSound);
                ShotGunSleeve.SetActive(true);
                yield return new WaitForSeconds(0.35f);
                ShotGunSleeve.SetActive(false);
                yield return new WaitForSeconds(0.25f);
                spareAmmo--;
                gunAmmo++;
                gunAmmo = Mathf.Clamp(gunAmmo, 0, MaxGunAmmo);
                AmmoText.text = gunAmmo + " / " + spareAmmo;
            }

            yield return new WaitForSeconds(0.23f);
            audioSource.PlayOneShot(ReloadEndSound);
            yield return new WaitForSeconds(0.43f);
        }
        else
        {
            animator.SetInteger("Status", 7);
            yield return new WaitForSeconds(0.25f);
            audioSource.PlayOneShot(ReloadSound);
            yield return new WaitForSeconds(ReloadInterval);
            spareAmmo -= reloadAmmo;
            gunAmmo += reloadAmmo;
            gunAmmo = Mathf.Clamp(gunAmmo, 0, MaxGunAmmo);
            AmmoText.text = gunAmmo + " / " + spareAmmo;
        }
        animator.SetInteger("Status", 0);
        reloading = false;
    }

    IEnumerator HideTimer()
    {
        StopCoroutine(_someCoroutine);
        shoting = false;
        reloading = false;
        hiding = true;
        audioSource.Stop();
        animator.SetInteger("Status", 0);
        animator.SetTrigger("Hide");
        yield return new WaitForSeconds(HideInterval + 0.25f);
        player.GetWeapon();
        hideFinishedFlag = true;
        hiding = false;
        this.gameObject.SetActive(false);
    }

    IEnumerator CrossHairManageTimer()
    {
        int count = 0;
        bool killingStatusFlag = false;
        bool attackingStatusFlag = false;
        while (true)
        {
            yield return new WaitForSeconds(0.01f);
            //bool centerFlag = false;
            //bool rightFlag = false;
            //bool leftFlag = false;
            // ノーマルクロスヘアの管理
            Ray ray = new Ray(Trajectory.transform.position, Trajectory.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, ShotRange) && hit.collider != null)
            {
                if (hit.collider.gameObject.tag == "Enemy" || hit.collider.gameObject.tag == "Robot")
                {
                    // centerFlag = true;

                    Vector3 relativePos = hit.collider.gameObject.transform.position - Player.transform.position;
                    relativePos = new Vector3(relativePos.x, 0f, relativePos.z); // 敵の方向を向く時に軸が傾く挙動を修正
                    Quaternion rotation = Quaternion.LookRotation(relativePos);
                    Player.transform.rotation = Quaternion.Slerp(Player.transform.rotation, rotation, 100f * Time.deltaTime);
                    gameDirector.SetColorNormalCrossHair(1); // 赤
                }
                else
                {
                    gameDirector.SetColorNormalCrossHair(0); // 白
                }
            }
            else
            {
                gameDirector.SetColorNormalCrossHair(0); // 白
            }

            // アタック・キルクロスヘアの管理
            if (killShotFlag) // キルの場合
            {
                killingStatusFlag = true;
                killShotFlag = false;
                gameDirector.SetAttackKillCrossHair(2);
                count = 0;
            }
            else if (attackShotFlag) // ダメージを与えた場合
            {
                attackingStatusFlag = true;
                attackShotFlag = false;
                gameDirector.SetAttackKillCrossHair(1);
                count = 0;
            }
            else
            {
                if (killingStatusFlag || attackingStatusFlag)
                {
                    count++;
                    if (count >= 50)
                    {
                        killingStatusFlag = false;
                        attackingStatusFlag = false;
                    }
                }
                else
                {
                    gameDirector.SetAttackKillCrossHair(0);
                }
            }

            // オートエイム機能
            /*
            Vector3 rightPos = new Vector3(Trajectory.transform.position.x + 0.5f, Trajectory.transform.position.y, Trajectory.transform.position.z);
            Vector3 leftPos = new Vector3(Trajectory.transform.position.x - 0.5f, Trajectory.transform.position.y, Trajectory.transform.position.z);
            Ray rightRay = new Ray(rightPos, Trajectory.transform.forward);
            Ray leftRay = new Ray(leftPos, Trajectory.transform.forward);
            RaycastHit rightHit;
            RaycastHit leftHit;
            //UnityEngine.Debug.DrawRay(rightRay.origin, rightRay.direction * 100, Color.red, 1f, false);
            //UnityEngine.Debug.DrawRay(leftRay.origin, leftRay.direction * 100, Color.red, 1f, false);

            if (Physics.Raycast(rightRay, out rightHit, ShotRange) && rightHit.collider != null)
            {
                if (rightHit.collider.gameObject.tag == "RedTeam" || rightHit.collider.gameObject.tag == "GreenTeam")
                    rightFlag = true;
            }
            if (Physics.Raycast(leftRay, out leftHit, ShotRange) && leftHit.collider != null)
            {
                if (leftHit.collider.gameObject.tag == "RedTeam" || leftHit.collider.gameObject.tag == "GreenTeam")
                    leftFlag = true;
            }
            /*
            if (!centerFlag && rightFlag)
                Player.transform.Rotate(0, 50f * Time.deltaTime, 0);
            if (!centerFlag && leftFlag)
                Player.transform.Rotate(0, -50f * Time.deltaTime, 0);
            if (centerFlag && rightFlag)
                Player.transform.Rotate(0, 50f * Time.deltaTime, 0);
            if (centerFlag && leftFlag)
                Player.transform.Rotate(0, -50f * Time.deltaTime, 0);
            */
        }
    }
    // スコープのズームセット、ズームインが完了している場合のみtrueを返す
    bool CameraAimZoomSet(bool ONOFF)
    {
        float mcfov = MainCamera.fieldOfView;
        //ズームインする場合
        if (ONOFF)
        {
            // 既にズームインが完了しているなら
            if (mcfov == AimZoom)
                return true;
            // ズームインが途中であれば
            else
                MainCamera.fieldOfView = Mathf.Clamp((mcfov + (AimZoom - originalZoom) * Time.deltaTime * ZoomInSpeed), AimZoom, originalZoom);
        }
        // ズームアウトする場合
        else
        {
            // 既にズームアウトが完了しているなら
            if (mcfov == originalZoom)
                return false;
            // ズームアウトが途中であれば
            else
                MainCamera.fieldOfView = Mathf.Clamp((mcfov - (AimZoom - originalZoom) * Time.deltaTime * ZoomOutSpeed), AimZoom, originalZoom);
        }
        return false;
    }

    // スコープのぞき込み時のサブカメラディスプレイセット
    void ScopeCameraSet(bool ONOFF)
    {
        if(SniperAimImage != null && SniperCamera != null)
        {
            if (ONOFF)
            {
                SniperAimImage.SetActive(true);
                SniperCamera.SetActive(true);
                mainCamera_Camera.enabled = false;
            }
            else
            {
                SniperAimImage.SetActive(false);
                SniperCamera.SetActive(false);
                mainCamera_Camera.enabled = true;
            }
        }
    }

    public bool GetshotingFlag()
    {
        return shoting;
    }
    
    public bool GetreloadingFlag()
    {
        return reloading;
    }

    public bool Getgetting()
    {
        return getting;
    }

    public bool GethideFinishedFlag()
    {
        bool flag = hideFinishedFlag;
        hideFinishedFlag = false;
        return flag;
    }

    public void SetAnimatorStatusIdle()
    {
        shoting = false; 
        reloading = false; 
        hiding = false;
        getting = false;
        animator.SetInteger("Status", 0);
        audioSource.Stop();
    }

    public void StartHideWeapon()
    {
        startHideWeaponFlag = true;
    }

    public void InitializeAmmo()
    {
        spareAmmo = MaxSpareAmmo;
        gunAmmo = MaxGunAmmo;
    }
    public void AddAmmo()
    {
        spareAmmo += MaxGunAmmo;
        AmmoText.text = gunAmmo + " / " + spareAmmo;
    }

    public void CancelReload()
    {
        reloading = false;
        //Idle
        animator.SetInteger("Status", 0);

    }
}