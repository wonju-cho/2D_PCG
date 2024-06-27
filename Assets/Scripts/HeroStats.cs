/*******************************************************************************
File:      HeroStats.cs
Author:    Victor Cecci
DP Email:  victor.cecci@digipen.edu
Date:      12/5/2018
Course:    CS186
Section:   Z

Description:
    This component is keeps track of all relevant hero stats. It also handles
    collisions with objects that would modify any stat.

    - MaxHealth = 3
    - Power = 1

*******************************************************************************/
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;

public class HeroStats : MonoBehaviour
{
    //Hero Stats
    public GameObject MainCameraPrefab;
    public GameObject WeightedCameraTargetPrefab;
	public GameObject TimedAnchorPrefab;
    public GameObject UiCanvasPrefab;
    private UiStatsDisplay HeroStatsDisplay;
    public ParticleSystem ShieldEffect;

    public Text ShotBoostTEXT;
    public Text ShotSpeedBoostTEXT;
    public Text SpeedTEXT;
    public Text GoldKeyTEXT;


    private int speedCount = 0;
    private int shotBoostCount = 0;
    private int shotSpeedBoostCount = 0;

    [SerializeField] private float ShieldCoolDown = 4.0f;
    private bool ShieldCondition = false;

    public bool FirstPan = false;
    
    bool DashAbility = false;


    public int StartingHealth = 3;
    public int MaxHealth
    {
        get { return _MaxHealth; }

        set
        {
            HeroStatsDisplay.HealthBarDisplay.MaxHealth = value;
            _MaxHealth = value;
        }
    }
    private int _MaxHealth;

    public int Health
    {
        get { return _Health; }

        set
        {
            HeroStatsDisplay.HealthBarDisplay.Health = value;
            _Health = value;
        }

    }
    private int _Health;

    public int StartingSilverKeys = 0;
	[HideInInspector]
    public int SilverKeys;

    public int StartingGoldKeys = 0;
	[HideInInspector]
    public int GoldKeys;

    public int StartingExitKeys = 0;
    [HideInInspector]
    public int ExitKeys;

    public int StartingSpeed = 5;
	[HideInInspector]
    public int Speed;

    AudioSource audioSource;
    public AudioClip ammoUpClip;
    public AudioClip hpClip;
    public AudioClip shieldClip;
    public AudioClip doorUnlockClip;

    [SerializeField] private float DashCooldown = 2.0f;

    private bool Once = false;

    public HeroShoot HS;

    private List<string> bossEnemyNames;

    GameObject Boss;
    // Start is called before the first frame update
    void Start()
    {

        GoldKeys = 0;
        ShotBoostTEXT.text = shotBoostCount.ToString();
        ShotSpeedBoostTEXT.text = shotSpeedBoostCount.ToString();
        SpeedTEXT.text = speedCount.ToString();
        GoldKeyTEXT.text = GoldKeys.ToString();

        //Spawn canvas
        var canvas = Instantiate(UiCanvasPrefab);
        HeroStatsDisplay = canvas.GetComponent<UiStatsDisplay>();

        //Spawn main camera
        var wct = Instantiate(WeightedCameraTargetPrefab);
        var cam = Instantiate(MainCameraPrefab);
        cam.GetComponent<CameraFollow>().ObjectToFollow = wct.transform;
		
        //Initialize stats
        MaxHealth = StartingHealth;
        Health = MaxHealth;
        SilverKeys = StartingSilverKeys;
        GoldKeys = StartingGoldKeys;
        Speed = StartingSpeed;

        audioSource = GetComponent<AudioSource>();

        Boss = new GameObject("Boss");

        if (!audioSource)
            Debug.Log("No AudioSource");

        if(!ammoUpClip)
            Debug.Log("No ammoUpClip");
        
        if(!hpClip)
            Debug.Log("No hpClip");

        if (!shieldClip)
            Debug.Log("No shieldClip");

        HS = GetComponent<HeroShoot>();

        if (!HS)
            Debug.Log("No HeroShoot");

        bossEnemyNames = new List<string>
        {
            "TankBossEnemy(Clone)",
            "FastBossEnemy(Clone)",
            "BossEnemy(Clone)",
        };
    }

    public IEnumerator ChangeCondition(float coolDown)
    {
        yield return new WaitForSeconds(coolDown);
        
        ShieldCondition = false;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < bossEnemyNames.Count; i++)
        {
            Boss = GameObject.Find(bossEnemyNames[i])/*.transform.position*/;
        }

        if (FirstPan == true)
        {
            var ta = Instantiate(TimedAnchorPrefab);

            for(int i =0; i < bossEnemyNames.Count; i++)
            {
                Boss = GameObject.Find(bossEnemyNames[i])/*.transform.position*/;

                if(Boss)
                {
                    ta.transform.position = Boss.transform.position;
                    break;
                }
            }
            FirstPan = false;
        }


        if (ShieldCondition)
        {
            StartCoroutine(ChangeCondition(ShieldCoolDown));
        }

        if (Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}

    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Check collision against collectibles
        var collectible = collision.gameObject.GetComponent<CollectibleLogic>();
        if (collectible != null)
        {
			GameObject go;
            //Increment relevant stat baed on Collectible type
            switch (collectible.Type)
            {
                case CollectibleTypes.HealthBoost:
                    ++MaxHealth;
                    Health = MaxHealth;
                    audioSource.PlayOneShot(hpClip, 1);
                    break;
                case CollectibleTypes.SilverKey:
                    ++SilverKeys;
                    audioSource.PlayOneShot(doorUnlockClip);

                    if(!Once)
                    {
                        go = Instantiate(TimedAnchorPrefab);
                        go.transform.position = GameObject.Find(Boss.name).transform.position;
                        Once = true;
                    }
					
					GameObject[] silverdoors = GameObject.FindGameObjectsWithTag("SilverDoor");
					foreach(GameObject silverdoor in silverdoors)
						GameObject.Destroy(silverdoor);
                    break;
                case CollectibleTypes.GoldKey:
                    ++GoldKeys;
                    GoldKeyTEXT.text = GoldKeys.ToString();
                    GameObject[] golddoors = GameObject.FindGameObjectsWithTag("GoldDoor");
                    ShieldCondition = true;
                    if(GoldKeys == 2)
                    {
                        go = Instantiate(TimedAnchorPrefab);
                        go.transform.position = GameObject.Find("Portal(Clone)").transform.position;
                        audioSource.PlayOneShot(doorUnlockClip);

                        foreach (GameObject golddoor in golddoors)
                            GameObject.Destroy(golddoor);
                    }
                    break;
                case CollectibleTypes.ExitKey:
                    ++ExitKeys;
                    audioSource.PlayOneShot(doorUnlockClip);
                    GameObject[] exitdoors = GameObject.FindGameObjectsWithTag("ExitDoor");
                    foreach (GameObject golddoor in exitdoors)
                        GameObject.Destroy(golddoor);
                    break;
                case CollectibleTypes.SpeedBoost:
                    ++Speed;
                    speedCount++;
                    SpeedTEXT.text = speedCount.ToString();
                    audioSource.PlayOneShot(hpClip, 1);
                    break;
                case CollectibleTypes.ShotBoost:
                    shotBoostCount++;
                    ShotBoostTEXT.text = shotBoostCount.ToString();
                    ++(GetComponent<HeroShoot>().BulletsPerShot);
                    audioSource.PlayOneShot(ammoUpClip);
                    break;
                case CollectibleTypes.Heart:
                    if (Health == MaxHealth)
                        return;
                    ++Health;
                    audioSource.PlayOneShot(hpClip, 1);
                    break;
                case CollectibleTypes.Shield:
                    PlayShieldParticle();
                    ShieldCondition = true;
                    audioSource.PlayOneShot(shieldClip);
                    break;
                case CollectibleTypes.ShotSpeedBoost:
                    shotSpeedBoostCount++;
                    ShotSpeedBoostTEXT.text = shotSpeedBoostCount.ToString();
                    HS.BulletSpeed += 2.0f;
                    audioSource.PlayOneShot(ammoUpClip);
                    break;
                case CollectibleTypes.DashAbility:
                    DashAbility = true;
                    break;
            }

            //Destroy collectible
            Destroy(collectible.gameObject);

        }//Collectibles End

        //Check collsion against enemy bullets
        var bullet = collision.GetComponent<BulletLogic>();
        if (bullet != null && bullet.Team == Teams.Enemy && !ShieldCondition)
        {
            if (bullet.name == "BossEnemyBullet(Clone)")
            {
                Health -= 2;
            }
            else 
                Health -= 1;

            if (Health <= 0)
            {  
                gameObject.SetActive(false);
                Invoke("ResetLevel", 1.5f);
            }
        }
    
    }

    public bool GetDashAbility()
    {
        return DashAbility;
    }

    public void ResetLevel()
    {
        var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    void PlayShieldParticle()
    {
        ShieldEffect.Play();
    }


}
