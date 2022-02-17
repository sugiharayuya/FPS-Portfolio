using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class Explosion : MonoBehaviour
{
	[SerializeField]
	bool GrenadeFlag = false; // グレネードの場合、敵へのダメージをゼロとする（今回のルールのみ）
	[SerializeField] 
	int DamageToPlayer;
	[SerializeField] 
	int DamageToEnemy;
	[SerializeField] 
	AudioClip ExplosionSound;

	bool Flag = true;
	GameDirector gameDirector;
	Player player;
	List<GameObject> enemyList = new List<GameObject>() {null };
	List<GameObject> playerList = new List<GameObject>() {null };

	void OnEnable()
	{
		gameDirector = GameObject.FindGameObjectWithTag("GameDirector").GetComponent<GameDirector>();
		player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
		StartCoroutine(ExplosionEffectTimer());
		if (GrenadeFlag) DamageToEnemy = 0;
	}

	IEnumerator ExplosionEffectTimer()
	{
		GetComponent<AudioSource>().PlayOneShot(ExplosionSound);
		int count = 0;
		while (count < 300)
		{
			yield return new WaitForSeconds(0.01f);
			count++;
			if (Flag && count > 3)
			{
				Flag = false;
				GetComponent<SphereCollider>().enabled = false;
				// 爆風圏内にいたキャラ全てに対して
				for (int i = 1; i < enemyList.Count; i++) // i=1から始めることによって0番目のnullを無視
				{
					if(enemyList[i].GetComponent<Character>() != null)
					{
						bool flag = enemyList[i].GetComponent<Character>().TakeDamageToTarget(DamageToEnemy);
                        if (flag)
						{
							gameDirector.SetAttackKillCrossHair(2); // キルの場合
							player.ChangeToNextWeapon();
						}
						else
							gameDirector.SetAttackKillCrossHair(1); // ダメージの場合
					}
				}
				if (playerList.Count >= 2)
					playerList[1].GetComponent<Player>().TakeDamageToPlayer(DamageToPlayer, transform.position.x, transform.position.z);
			}
		}
		// 3秒後に爆発エフェクトを削除
		GameObject.Destroy(this.gameObject);

	}

	void OnTriggerStay(Collider other)
    {
        if (Flag)
        {
			if (other.gameObject.tag == "Enemy")
			{
				for (int i = 0; i < enemyList.Count; i++)
				{
					if (other.gameObject == enemyList[i])
						break;
					if (i == enemyList.Count - 1)
						enemyList.Add(other.gameObject);
				}
			}

			if(other.gameObject.tag == "Player")
			{
				playerList.Add(other.gameObject);
			}
		}
    }


}
