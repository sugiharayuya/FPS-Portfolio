using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrenadeHands : MonoBehaviour
{


    public float GetInterval;
    public float HideInterval;
    public float ThrowInterval;

    [SerializeField]
    GameObject Player = null; // プレイヤー参照
    [SerializeField]
    Text GrenadeText = null; // グレネードテキスト参照
    [SerializeField]
    GameObject GrenadePrefab = null; // グレネードプレハブ参照
    [SerializeField]
    GameObject ThrowPoint = null;
    [SerializeField]
    GameObject BarrierForShotButton = null; // ショットボタン保護バリア
    [SerializeField]
    GameObject ThrowGrenadeButton = null; // スローグレネードボタン
    public AudioClip PullPinSound; // 射撃音参照
    bool getting = false;
    bool hiding = false;
    bool throwing = false;
    bool throwGrenadeButtonDownFlag = false;
    Animator animator;
    AudioSource audioSource;
    Player player;


    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        player = Player.GetComponent<Player>();
    }

    void OnEnable()
    {
        StartCoroutine(GetTimer());
    }
    void OnDisable()
    {
        throwGrenadeButtonDownFlag = false;
    }
    // Update is called once per frame
    void Update()
    {

        if (!getting && !hiding && !throwing)
        {
            if (throwGrenadeButtonDownFlag)
            {
                StartCoroutine(ThrowTimer());
            }
            else
            {
                int jsFlag = Player.GetComponent<Player>().GetjsFlag();
                if (jsFlag == 1)
                {
                    //Idle
                    animator.SetInteger("Status", 0);
                }
                else if (jsFlag == 2)
                {
                    //Walk
                    animator.SetInteger("Status", 1);
                }
                else
                {
                    //Run
                    animator.SetInteger("Status", 2);
                }
            }
        }
    }

    IEnumerator GetTimer()
    {
        getting = true;
        BarrierForShotButton.SetActive(true);
        ThrowGrenadeButton.SetActive(true);
        yield return new WaitForSeconds(GetInterval + 0.25f);
        getting = false;

    }
    IEnumerator HideTimer()
    {
        hiding = true;
        animator.SetInteger("Status", 0);
        animator.SetTrigger("Hide");
        yield return new WaitForSeconds(HideInterval + 0.25f);
        player.GetWeapon();
        hiding = false;
        BarrierForShotButton.SetActive(false);
        ThrowGrenadeButton.SetActive(false);
        this.gameObject.SetActive(false);
    }
    IEnumerator ThrowTimer()
    {
        throwing = true;
        animator.SetInteger("Status", 0);
        animator.SetTrigger("Throw");
        audioSource.PlayOneShot(PullPinSound);
        yield return new WaitForSeconds(ThrowInterval - 0.3f);
        // ここで手榴弾を前方に飛ばす
        GameObject grenade = Instantiate(GrenadePrefab, ThrowPoint.transform.position, Quaternion.Euler(90, 0, 0));
        grenade.GetComponent<Rigidbody>().AddForce(ThrowPoint.transform.forward * 3000);
        player.GrenadeNum--;
        GrenadeText.text = player.GrenadeNum.ToString();
        yield return new WaitForSeconds(0.3f);
        player.GetWeapon();
        throwing = false;
        BarrierForShotButton.SetActive(false);
        ThrowGrenadeButton.SetActive(false);
        this.gameObject.SetActive(false);
    }

    public void StartHideGrenade()
    {
        if (!hiding && !throwing)
        {
            StartCoroutine(HideTimer());
        }
    }
    public void ThrowGrenadeButtonDown()
    {
        throwGrenadeButtonDownFlag = true;
    }
}
