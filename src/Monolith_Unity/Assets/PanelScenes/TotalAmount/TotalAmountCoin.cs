using System;
using UnityEngine;

public enum CoinType
{
    Coin1,
    Coin2,
    Coin3,
    Coin4,
}


public class TotalAmountCoin : MonoBehaviour
{

    public float initialXForceMax = 10000;
    public float initialYForce = 1000;
    public float initialTorque = 500f;

    public float movementThreshold = 0.01f; // world units
    public float settleTime = 0.5f;          // seconds

    public float maxLifeTime = 15;
    public SpriteRenderer MainSprite;
    public SpriteRenderer BorderSprite;
    public SpriteRenderer LogoSprite;

    public Color Coin2MainColor;
    public Color Coin2BorderColor;
    public Color Coin3MainColor;
    public Color Coin3BorderColor;
    public Color Coin4MainColor;
    public Color Coin4BorderColor;

    public CoinType CoinType { get; private set; }

    private float startTime = 0;
    private Vector2 lastPosition;
    private float stillTime;

    public CoinSpawner Spawner { get; set; }
    private Rigidbody2D rb;
    private CollisionDetectionMode2D rb_CollisionDetectionMode2D;
    private PhysicsMaterial2D rb_PhysicsMaterial2D;
    private RigidbodyType2D rb_RigidbodyType2D;
    private float rb_linearDamping;
    private float rb_angularDamping;
    private float rb_gravityScale;


    private RigidbodyInterpolation2D rb_RigidbodyInterpolation2D;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        rb_PhysicsMaterial2D = rb.sharedMaterial;
        rb_RigidbodyType2D = rb.bodyType;
        rb_linearDamping = rb.linearDamping;
        rb_angularDamping = rb.angularDamping;
        rb_gravityScale = rb.gravityScale;
        rb_CollisionDetectionMode2D = rb.collisionDetectionMode;
        rb_RigidbodyInterpolation2D = rb.interpolation;
        AddInitialForce();
        lastPosition = rb.position;
        startTime = Time.time;
    }


    public void UpgradeCoinType()
    {
        switch (this.CoinType)
        {
            case CoinType.Coin1:
                SetCoinType(CoinType.Coin2);
                break;
            case CoinType.Coin2:
                SetCoinType(CoinType.Coin3);
                break;
            case CoinType.Coin3:
                SetCoinType(CoinType.Coin4);
                break;
            case CoinType.Coin4:
                break;
        }
    }

    public void SetCoinType(CoinType coinType)
    {
        this.CoinType = coinType;
        switch (coinType)
        {
            case CoinType.Coin1:
                transform.localScale = Vector3.one * 10;
                break;
            case CoinType.Coin2:
                transform.localScale = Vector3.one * 20;
                this.MainSprite.color = Coin2MainColor;
                this.BorderSprite.color = Coin2BorderColor;
                break;
            case CoinType.Coin3:
                transform.localScale = Vector3.one * 30;
                this.MainSprite.color = Coin3MainColor;
                this.BorderSprite.color = Coin3BorderColor;
                break;
            case CoinType.Coin4:
                transform.localScale = Vector3.one * 55;
                this.MainSprite.color = Coin4MainColor;
                this.BorderSprite.color = Coin4BorderColor;
                this.LogoSprite.color = Color.white;
                break;
        }
    }


    void FixedUpdate()
    {

        if (rb.bodyType != RigidbodyType2D.Dynamic)
            return;

        Vector2 currentPosition = rb.position;
        float sqrMoved = (currentPosition - lastPosition).sqrMagnitude;

        if (sqrMoved < movementThreshold * movementThreshold)
        {
            stillTime += Time.fixedDeltaTime;

            if (stillTime >= settleTime)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static;
                Destroy(rb);
                this.enabled = false;
                Spawner.CoinIsNoLongerActive(this);
            }
        }
        else
        {
            stillTime = 0f;
        }

        lastPosition = currentPosition;

        if (Time.time - startTime > maxLifeTime)
        {
            startTime = Time.time;
            transform.position = Spawner.GetRandomPosition();
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (transform.position.x > 2000 || transform.position.x < -2000 ||
            transform.position.y > 2000 || transform.position.y < -2000 ||
            transform.position.z > 2000 || transform.position.z < -2000)
        {
            startTime = Time.time;
            transform.position = Spawner.GetRandomPosition();
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }


    public void ReEnable()
    {
        this.enabled = true;
        BoxCollider2D[] boxColliders = GetComponentsInChildren<BoxCollider2D>();
        for (int i = 0; i < boxColliders.Length; i++)
        {
            boxColliders[i].compositeOperation = Collider2D.CompositeOperation.None;
        }

        this.rb = gameObject.AddComponent<Rigidbody2D>();

        rb.sharedMaterial = rb_PhysicsMaterial2D;
        rb.bodyType = rb_RigidbodyType2D;
        rb.linearDamping = rb_linearDamping;
        rb.angularDamping = rb_angularDamping;
        rb.gravityScale = rb_gravityScale;
        rb.collisionDetectionMode = rb_CollisionDetectionMode2D;
        rb.interpolation = rb_RigidbodyInterpolation2D;
        startTime = Time.time;
        stillTime = 0;
        MakeJump();
    }

    public void MakeJump()
    {
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        stillTime = 0;
        AddInitialForce();
    }

    private void AddInitialForce()
    {
        float initialXForce = UnityEngine.Random.Range(-initialXForceMax, initialXForceMax);
        rb.AddForceX(initialXForce);
        rb.AddForceY(initialYForce);
        int rotationDirection = initialXForce > 0 ? -1 : 1;
        rb.AddTorque(initialTorque * rotationDirection);
    }

    internal void SetAlpha(float alpha)
    {
        var color = MainSprite.color;
        color.a = alpha;
        MainSprite.color = color;

        color = BorderSprite.color;
        color.a = alpha;
        BorderSprite.color = color;

        if(CoinType == CoinType.Coin4)
        {
            color = LogoSprite.color;
            color.a = alpha;
            LogoSprite.color = color;
        }
    }
}
