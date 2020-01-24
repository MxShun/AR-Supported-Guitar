using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CodesManager : MonoBehaviour {
    // 時間制限
    public float timeOut = 120.0f; // 一定時間
    private float timeElapsed = 0.0f;
    public Image Timegauge;
    // アクティブコード
    private GameObject code;
    private static string codename;
    // 成功回数による変更フラグ
    private static bool change = false;

    private GameObject Feedback;

    // Use this for initialization
    void Start ()
    {
        Feedback = GameObject.Find("Feedback");

        codename = "C"; // デフォルトは"C"
	}
	
	// Update is called once per frame
	void Update ()
    {
        timeElapsed += Time.deltaTime; // 経過時間を加算
        Timegauge.fillAmount = 1.0f - (timeElapsed / timeOut);

        if (timeElapsed >= timeOut || change) // 一定時間を経過
        {
            /*
             * 子要素を全て非アクティブ化
             */
            foreach(Transform child in this.transform)
            {
                child.gameObject.SetActive(false);
            }

            /*
             * ランダムで1つの子要素をアクティブ化
             */
            if((int)Time.time % this.transform.childCount == 0)
            {
                code = this.transform.Find("A").gameObject;
                code.SetActive(true);
                codename = "A";
            }
            else if ((int)Time.time % this.transform.childCount == 1)
            {
                code = this.transform.Find("B").gameObject;
                code.SetActive(true);
                codename = "B";
            }
            else if ((int)Time.time % this.transform.childCount == 2)
            {
                code = this.transform.Find("C").gameObject;
                code.SetActive(true);
                codename = "C";
            }
            else if ((int)Time.time % this.transform.childCount == 3)
            {
                code = this.transform.Find("D").gameObject;
                code.SetActive(true);
                codename = "D";
            }
            else if ((int)Time.time % this.transform.childCount == 4)
            {
                code = this.transform.Find("E").gameObject;
                code.SetActive(true);
                codename = "E";
            }
            else if ((int)Time.time % this.transform.childCount == 5)
            {
                code = this.transform.Find("F").gameObject;
                code.SetActive(true);
                codename = "F";
            }
            else
            {
                code = this.transform.Find("G").gameObject;
                code.SetActive(true);
                codename = "G";
            }

            // 制限時間の初期化
            timeElapsed = 0.0f;
            Timegauge.fillAmount = 1;
            // 成功回数による変更フラグの初期化
            change = false;
            // 成功回数の初期化
            Feedback.GetComponent<FeedbackManager>().RefreshGoodcounter();
        }
	}

    // アクティブコードの取得
    public string getCode()
    {
        return codename;
    }

    // コードの変更
    public void changeCode()
    {
        change = true;
    }
}
