using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Character : MonoBehaviour
{
    // Soldierのデータ
    //    Distance  damage  rate   killtime_HP200 (t = 200 * rate / damage)
    //SG     25       20     1.0         10s
    //SMG    30        4     0.15        7.5s
    //AR1    35        6     0.15        5s
    //AR2    40        8     0.15        3.75s
    //SR    100       100     2.0         4s
    [SerializeField]
    Transform Target;
    [SerializeField] 
    int MaxHp;
    [SerializeField] 
    bool FullAuto;
    [SerializeField] 
    bool SemiAuto;
    [SerializeField] 
    int ShotDistance;
    [SerializeField] 
    float ShotInterval;
    [SerializeField] 
    int Damage;
    [SerializeField] 
    GameObject Trajectory;
    [SerializeField] 
    GameObject Muzzle;
    [SerializeField] 
    GameObject MuzzleFlashPrefab;
    [SerializeField]
    GameObject GameDirector = null; // ゲームディレクター
    [SerializeField]
    AudioClip WalkSound = null; // 足音（walk）参照
    [SerializeField]
    AudioClip ShotSound = null; // 射撃音　参照

    bool ShotFlag = false; // 射程内にPlayerを見つけた場合
    bool moveEnabledFlag = true;
    bool FightFlag = false; // Playerを発見後、常にtrue
    int hp;

    Animator animator;
    NavMeshAgent agent;
    CapsuleCollider capsuleCollider;
    GameDirector gameDirector;
    AudioSource audioSource;
    Coroutine moveCoroutine;

    public int Hp
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

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        gameDirector = GameDirector.GetComponent<GameDirector>();
        Hp = MaxHp;
        moveCoroutine = StartCoroutine(MoveTimer());
    }

    // Update is called once per frame
    void Update()
    {

        if (ShotFlag)
        {
            // Playerの方向を向く
            transform.LookAt(Target.position);
        }

    }

    // 視界に相手がいるか探す
    IEnumerator MoveTimer()
    {
        while (true)
        {
            Vector3 targetPos = new Vector3(Target.position.x, Target.position.y + 1.9f, Target.position.z);
            Vector3 Vector = targetPos - Trajectory.transform.position;
            Ray ray = new Ray(Trajectory.transform.position, Vector);
            RaycastHit hit;
            // Playerに向かってレイを飛ばす(100m is OK)
            if (Physics.Raycast(ray, out hit, 100) && hit.collider != null)
            {
                // 戦闘状態でない場合、戦闘を開始する 
                if (hit.collider.gameObject.tag == "Player" && !FightFlag) FightFlag = true;
                // 戦闘状態の場合
                if (FightFlag)
                {
                    // Playerを射程内に発見した場合、射撃を実行
                    if (hit.collider.gameObject.tag == "Player" && Vector3.Distance(targetPos, Trajectory.transform.position) < ShotDistance)
                    {
                        // 追跡から射撃に切り替わる場合
                        if (!ShotFlag)
                        {
                            // 射撃フラグをtrue
                            ShotFlag = true;
                            // 追跡を停止する
                            agent.isStopped = true;
                            agent.velocity = Vector3.zero;
                            // Idleアニメーション
                            animator.SetInteger("Status", 0);
                            /*
                            float angle_x;
                            if (transform.localEulerAngles.x > 180) angle_x = transform.localEulerAngles.x - 360f;
                            else angle_x = transform.localEulerAngles.x;
                            transform.rotation = Quaternion.Euler(Mathf.Clamp(angle_x, -20, 20), transform.localEulerAngles.y, transform.localEulerAngles.z);
                            */
                            yield return new WaitForSeconds(0.8f); // Playerを発見してから射撃に入るまでの遅延
                        }
                        // 射撃が続いている場合
                        else
                        {
                            /*
                            float angle_x;
                            if (transform.localEulerAngles.x > 180) angle_x = transform.localEulerAngles.x - 360f;
                            else angle_x = transform.localEulerAngles.x;
                            transform.rotation = Quaternion.Euler(Mathf.Clamp(angle_x, -20, 20), transform.localEulerAngles.y, transform.localEulerAngles.z);
                            */

                            yield return new WaitForSeconds(0.1f);
                            // Shotアニメーションを実行
                            if (FullAuto) animator.SetInteger("Status", 2);
                            if (SemiAuto) animator.SetInteger("Status", 3);
                            // ショットサウンドを実行
                            audioSource.PlayOneShot(ShotSound);
                            // マズルフラッシュの生成
                            GameObject muzzleFlash = Instantiate(MuzzleFlashPrefab, Muzzle.transform.position, Muzzle.transform.rotation, Muzzle.transform);
                            muzzleFlash.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                            // 実際のダメージを与えるためのray（索敵用と区別）
                            if(Physics.Raycast(ray, out hit, ShotDistance) && hit.collider != null)
                            {
                                if(hit.collider.gameObject.tag == "Player")
                                    hit.collider.gameObject.GetComponent<Player>().TakeDamageToPlayer(Damage, transform.position.x, transform.position.z);
                            }
                            // ShotIntervalの7/8をShotアニメーション
                            yield return new WaitForSeconds(ShotInterval * 7f / 8f - 0.1f);
                            // ShotIntervalの1/8をIdleアニメーション
                            animator.SetInteger("Status", 0);
                            yield return new WaitForSeconds(ShotInterval / 8f);
                        }
                    }
                    // Playerが視界にいない場合、追跡する
                    else
                    {
                        // 射撃フラグをfalse
                        ShotFlag = false;
                        // Playerとの距離が10m以上離れている場合
                        if (Vector3.Distance(Target.position, Trajectory.transform.position) >= 10)
                        {
                            // 追跡を開始する
                            agent.isStopped = false;
                            // ターゲットの位置を更新
                            agent.SetDestination(Target.position);
                            // Runアニメーションを実行
                            animator.SetInteger("Status", 1);
                            yield return new WaitForSeconds(0.3f);
                            // Walkサウンドを実行
                            audioSource.PlayOneShot(WalkSound);
                        }
                        // Playerとの距離が10m未満の場合
                        else
                        {
                            // 追跡を停止する
                            agent.isStopped = true;
                            // Idleアニメーションを実行
                            animator.SetInteger("Status", 0);
                            yield return new WaitForSeconds(0.3f);
                        }
                    }
                }
                // 戦闘状態でない場合（Playerを一度も発見していない場合）、待機
                else yield return new WaitForSeconds(0.5f);
            }
            else yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator DeathTimer()
    {
        // moveコルーチンを停止
        StopCoroutine(moveCoroutine);
        // 当たり判定を削除
        capsuleCollider.enabled = false;
        // navmeshの動作を停止
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        // deathアニメーションを実行
        animator.SetInteger("Status", 0);
        animator.SetTrigger("Death");
        // ドロップアイテムをインスタンス化
        //gameDirector.ActivateDropItems(transform.position.x, transform.position.y + 1f, transform.position.z);
        yield return new WaitForSeconds(1.2f);
        // navmeshの動作を完全に停止
        agent.enabled = false;
        // キャラクターを破壊
        Destroy(this.gameObject);
    }
    // ダメージを受ける関数
    public bool TakeDamageToTarget(int damage)
    {
        if(Hp > 0)
        {
            Hp -= damage;
            // MenuDirector.DamageCount += damage;
            FightFlag = true;
            if (Hp <= 0)
            {
                // MenuDirector.KillCount++;
                StartCoroutine(DeathTimer());
                return true;
            }
        }
        
        return false;
    }
}
