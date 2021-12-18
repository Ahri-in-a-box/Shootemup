using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity{

    [Header("Camera Setting")]
    [SerializeField]
    private Camera m_MainCamera = null;

    [Header("Speed Settings")]
    [SerializeField, Range(0.0f, 15.0f)]
    private float m_VerticalSpeed = 0.0f;
    [SerializeField, Range(0.0f, 15.0f)]
    private float m_HorizontalSpeed = 0.0f;

    [Header("Fire Settings")]
    [SerializeField, Range(50.0f, 1000.0f)]
    private float m_FireSpeed = 500.0f;
    [SerializeField]
    private GameObject m_FireObject = null;

    private System.Diagnostics.Stopwatch m_Stopwatch = null;
    private uint m_Score = 0;
    private uint m_Shield = 0;
    private uint m_BulletPerShoot = 1;

    public uint score => m_Score;
    public uint shield => m_Shield;

    public delegate void d_OnHPChange(Player me);
    public delegate void d_OnScoreChange(uint score);
    public event d_OnHPChange OnHPChange;
    public event d_OnScoreChange OnScoreChange;

    private Quaternion m_DefaultRotation;

    new void Awake(){
        base.Awake();
        //Verifier si la cam�ra est bien set up
        if(!m_MainCamera)
            Debug.LogError("No Camera for Player");
        if(!m_FireObject)
            Debug.LogError("No Object to fire for Player");
        m_Stopwatch = new System.Diagnostics.Stopwatch();

        Bullet.OnHit += OnBulletHit;

        m_DefaultRotation = transform.rotation;
    }

    // Start is called before the first frame update
    void Start(){

    }

    void OnBulletHit(uint hp){
        m_Score += 10;
        if(hp == 0)
            m_Score += 20;
        OnScoreChange?.Invoke(m_Score);
    }

    private void OnCollisionEnter(Collision collision){
        switch (collision.gameObject.tag){
            case "Enemy":
                if (m_Shield > 0)
                    m_Shield--;
                else
                    m_ActualHealth--;
                OnHPChange?.Invoke(this);
                break;
            case "Loot":
                Loot loot = collision.gameObject.GetComponent<Loot>();
                switch((eLoot)loot.LootType){
                    case eLoot.HEALTH:
                        m_ActualHealth += loot.value;
                        if(m_ActualHealth > m_MaxHealth)
                            m_ActualHealth = m_MaxHealth;
                        break;
                    case eLoot.SHIELD:
                        m_Shield += loot.value;
                        break;
                    case eLoot.GROWTH:
                        m_ActualHealth += loot.value;
                        m_MaxHealth += loot.value;
                        break;
                    case eLoot.AMPLIFIER:
                        m_BulletPerShoot += 1;
                        if(m_BulletPerShoot > 3)
                            m_BulletPerShoot = 3;
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }

        if(m_ActualHealth == 0)
            Destroy(gameObject);
    }

    void PlayerControl(){
        transform.rotation = m_DefaultRotation;

        //Input Management
        if(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q))
            transform.position -= transform.right * m_HorizontalSpeed * Time.deltaTime;
        if(Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            transform.position += transform.right * m_HorizontalSpeed * Time.deltaTime;
        if(Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z))
            transform.position += transform.up * m_VerticalSpeed * Time.deltaTime;
        if(Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            transform.position -= transform.up * m_VerticalSpeed * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Escape))
            GameManager.Instance.Play(GameManager.Instance.m_Pause_Status);
        if(Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Mouse0)){
            bool generate = false;
            //Check Firing
            if(m_Stopwatch.IsRunning){
                if(m_Stopwatch.ElapsedMilliseconds > m_FireSpeed){
                    generate = true;
                    m_Stopwatch.Reset();
                    m_Stopwatch.Start();
                }
            }else{
                generate = true;
                m_Stopwatch.Start();
            }

            //Fire if needed
            if(generate && Time.timeScale != 0f){
                switch (m_BulletPerShoot){
                    case 2:
                        GameObject bullet1 = Instantiate(m_FireObject);
                        GameObject bullet2 = Instantiate(m_FireObject);
                        bullet1.transform.position = transform.position;
                        bullet1.transform.position -= bullet1.transform.right * 0.5f;
                        bullet2.transform.position = transform.position;
                        bullet2.transform.position += bullet2.transform.right * 0.5f;
                        break;
                    case 3:
                        for(int i = 0; i < 3; i++){
                            GameObject bul = Instantiate(m_FireObject);
                            bul.transform.position = transform.position;
                            bul.transform.Rotate(0, 0, (i - 1) * 35);
                        }
                        break;
                    default:
                        GameObject bullet = Instantiate(m_FireObject);
                        bullet.transform.position = transform.position;
                        break;
                }
                
            }
        }else{
            m_Stopwatch.Reset();
        }

        //Position Management
        Vector3 screen_position = m_MainCamera.WorldToScreenPoint(transform.position);

        if(screen_position.x < 0)
            transform.position += transform.right * m_HorizontalSpeed * Time.deltaTime;
        if(screen_position.x > Screen.width)
            transform.position -= transform.right * m_HorizontalSpeed * Time.deltaTime;
        if(screen_position.y < 0)
                transform.position += transform.up * m_VerticalSpeed * Time.deltaTime;
        if(screen_position.y > Screen.height)
            transform.position -= transform.up * m_VerticalSpeed * Time.deltaTime;

        //Rotation Management
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q))
            transform.Rotate(transform.up, 20f);
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            transform.Rotate(transform.up, -20f);
    }

    // Update is called once per frame
    void Update(){
        PlayerControl();
    }

    private void OnDestroy(){
        Bullet.OnHit -= OnBulletHit;
    }
}
