/*******************************************************************************
File:      EnemyStats.cs
Author:    Victor Cecci
DP Email:  victor.cecci@digipen.edu
Date:      12/5/2018
Course:    CS186
Section:   Z

Description:
    This component controls all behaviors for enemies in the game.

*******************************************************************************/
using UnityEngine;
using System.Collections.Generic;

public class EnemyStats : MonoBehaviour
{
    public GameObject EnemyHealthBarPrefab;
    private GameObject HealthBar;
    private HealthBar HealthBarComp;

    public int StartingHealth = 3;

    private Dictionary<string, GameObject> Prefabs; //Dictionary of all PCG prefabs
    private List<string> bossEnemyNames;
    private List<string> hardEnemyNames;
    private List<string> itemNames;
    bool IsHardEnemy;
    bool IsPortalEnemy;
    bool IsDashEnemy;
    bool IsBoss = false;
    bool die = false;
    Vector2Int position;
    private static System.Random RNG;

    public int Health
    {
        get { return _Health; }

        set
        {
            HealthBarComp.Health = value;
            _Health = value;
        }

    }
    private int _Health;

    static EnemyStats()
    {
        RNG = new System.Random();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Initialize enemy health bar
        HealthBar = Instantiate(EnemyHealthBarPrefab);
        HealthBar.GetComponent<ObjectFollow>().ObjectToFollow = transform;
        HealthBarComp = HealthBar.GetComponent<HealthBar>();
        HealthBarComp.MaxHealth = StartingHealth;
        HealthBarComp.Health = StartingHealth;
        Health = StartingHealth;

        Prefabs = new Dictionary<string, GameObject>();
        Prefabs.Add("goldkey", Resources.Load<GameObject>("Prefabs/GoldKey"));
        Prefabs["goldkey"].transform.localScale = new Vector3(3.0f, 3.0f, 1.0f);
        Prefabs.Add("heart", Resources.Load<GameObject>("Prefabs/HeartPickup"));
        Prefabs.Add("healthboost", Resources.Load<GameObject>("Prefabs/HealthBoost"));
        Prefabs.Add("shotboost", Resources.Load<GameObject>("Prefabs/ShotBoost"));
        Prefabs.Add("shotspeedboost", Resources.Load<GameObject>("Prefabs/ShotSpeedBoost"));
        Prefabs.Add("speedboost", Resources.Load<GameObject>("Prefabs/SpeedBoost"));
        Prefabs.Add("shield", Resources.Load<GameObject>("Prefabs/Shield")); //add shield
        Prefabs.Add("silverkey", Resources.Load<GameObject>("Prefabs/SilverKey"));
        Prefabs["silverkey"].transform.localScale = new Vector3(3.0f, 3.0f, 1.0f);
        Prefabs.Add("exitkey", Resources.Load<GameObject>("Prefabs/ExitKey"));
        Prefabs["exitkey"].transform.localScale = new Vector3(3.0f, 3.0f, 1.0f);
        Prefabs.Add("dashability", Resources.Load<GameObject>("Prefabs/DashAbility"));

        RNG = new System.Random();

        bossEnemyNames = new List<string>()
        {
            "TankBossEnemy(Clone)",
            "FastBossEnemy(Clone)",
            "BossEnemy(Clone)",
        };

        hardEnemyNames = new List<string>()
        {
            "TankEnemy(Clone)",
            "UltraEnemy(Clone)",
        };


        itemNames = new List<string>()
        {
            "heart",
            "healthboost",
            "shotboost",
            "shotspeedboost",
            "shield",
            "speedboost",
        };

        for (int i = 0; i < bossEnemyNames.Count; i++)
        {
            if (gameObject.name == bossEnemyNames[i])
            {
                IsBoss = true;
                break;
            }
        }

        for (int i = 0; i < hardEnemyNames.Count; i++)
        {
            if (gameObject.name == hardEnemyNames[i])
            {
                IsHardEnemy = true;
                break;
            }
        }

        if (gameObject.name == "PortalEnemy")
            IsPortalEnemy = true;

        if (gameObject.name == "DashAbilityEnemy(Clone)")
            IsDashEnemy = true;

    }

    // Update is called once per frame
    void Update()
    {
    }

    GameObject Spawn(string obj, float x, float y)
    {
        return Instantiate(Prefabs[obj], new Vector3(x , y, 0.0f), Quaternion.identity);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        var bullet = col.GetComponent<BulletLogic>();
        if (bullet != null && bullet.Team == Teams.Player)
        {
            Health -= 1;
			GetComponent<EnemyChaseLogic>().Aggroed = true;

            if (Health <= 0)
            {
                die = true;
                position.x = (int)gameObject.transform.position.x;
                position.y = (int)gameObject.transform.position.y;

                if (IsBoss && die)
                {
                    Spawn("goldkey", position.x, position.y);
                }
                else if (IsHardEnemy && die)
                {
                    Spawn("silverkey", position.x, position.y);
                }
                else if(IsPortalEnemy && die)
                {
                    Spawn("exitkey", position.x, position.y);
                }
                else if(IsDashEnemy && die)
                {
                    Spawn("dashability", position.x, position.y);
                }
                else
                {
                    int randomNumber = RNG.Next(2);
                    if(randomNumber == 1)
                    {
                        Spawn(itemNames[RNG.Next(6)], position.x, position.y);
                    }
                }

                Destroy(gameObject);
            }
        }

        //Aggro on friendly fire
        if (bullet != null && bullet.Team != Teams.Player)
			GetComponent<EnemyChaseLogic>().Aggroed = true;
    }

    private void OnTriggerStay2D(Collider2D col)
    {
		//Aggro on friendly collision
        var enemy = col.GetComponent<EnemyChaseLogic>();
        if (enemy != null && enemy.Aggroed == true)
			GetComponent<EnemyChaseLogic>().Aggroed = true;
    }

    private void OnDestroy()
    {
        Destroy(HealthBar);
    }
}
