using System.Collections.Generic;
using UnityEngine;

public class UiObjectPool : MonoBehaviour
{
    public static UiObjectPool instance;

    [Header("Damage Text")] //Ui에 쓸 풀링은 DamageText 하나 
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private int poolSize = 30; //추후, 다른종류의 데미지텍스트가발생할경우 여기서 변경. 

    private Queue<GameObject> pool = new Queue<GameObject>();

    private Transform poolParent;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        // DamageText 정리
        GameObject parentPool = new GameObject("DamageText_Pool");
        parentPool.transform.SetParent(transform);

        poolParent = parentPool.transform;


        // 초기 풀 생성
        for (int i = 0; i < poolSize; i++)
        {
            GameObject damageText = Instantiate(
                damageTextPrefab,
                poolParent
            );

            damageText.SetActive(false);
            pool.Enqueue(damageText);
        }
    }


    public DamageText GetDamageText()
    {
        GameObject damageText;

        // 사용할 수 있는 객체가 있으면 꺼냄
        if (pool.Count > 0)
        {
            damageText = pool.Dequeue();
        }
        // 없으면 새로 생성
        else
        {
            damageText = Instantiate(
                damageTextPrefab,
                poolParent
            );
        }

        damageText.SetActive(true);

        return damageText.GetComponent<DamageText>();
    }


    public void ReturnDamageText(GameObject damageText)
    {
        damageText.SetActive(false);

        // 다시 풀에
        damageText.transform.SetParent(poolParent);

        pool.Enqueue(damageText);
    }
}


