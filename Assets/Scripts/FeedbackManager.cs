using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackManager : MonoBehaviour {

    // FFT分解能（2の累乗）
    private static int FFT_RESOLUTION = 2048;
    // 採用する最低周波域
    private static int FREQUENCY_RANGE = 450;
    // 極値を算出する幅
    private static int EXTREMUM_RANGE = 5;
    // 極小値-極大値の閾値
    private static float EXTREMUM_THRESHOLD = 0.0005f; //0.003f;
    // Feedback要素の更新頻度
    private static int CYCLE = 55;
    // 成功回数の上限値
    private static int MAX_GOODCOUNTER = 100;

    // オーディオソースの指定
    public AudioSource Mic;
    // Codesオブジェクト
    private GameObject Codes;
    // フレーム数
    private static int Phase = 0;
    // 成功回数
    private static int Goodcounter = 0;
    public Image Goodscale;
    // 各弦を正しく押さえているかの判定用
    private static bool string1 = false;
    private static bool string2 = false;
    private static bool string3 = false;
    private static bool string4 = false;
    private static bool string5 = false;
    private static bool string6 = false;

    /*
     * a ～ a + bでの最大値インデックスを取得する関数
     */
    public static int GetMaxNo(int a, int b, float[] c)
    {
        int maxNum = a;
        float max = c[a];
        for (int i = a; i < a + b && i < c.Length; i++)
        {
            if (c[i] > max)
            {
                max = c[i];
                maxNum = i;
            }
        }
        return maxNum;
    }

    /*
     * a ～ bでの最小値インデックスを取得する関数
     */
    public static int GetMinNo(int a, int b, float[] c)
    {
        int minNum = a;
        float min = c[a];
        for (int i = a; i < b; i++)
        {
            if (min > c[i])
            {
                min = c[i];
                minNum = i;
            }
        }
        return minNum;
    }

    /* 
     * FFT，スペクトル波形描画，ピーク値の算出，ピーク周波数を返す
     */
    public static float[] AnalyzeSound(AudioSource audio)
    {
        /*
         * FFT
         */
        float[] spectrum = new float[FFT_RESOLUTION];
        audio.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        /*
         * 周波数スペクトル波形をSceneに描画【テスト用】
         *
        for (int i = 1; i < spectrum.Length - 1; ++i)
        {
            Debug.DrawLine(
                    new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3),
                    new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3),
                    Color.yellow);
        }*/

        /*
         * ピーク値の算出
         */
        // 極大値インデックスの配列
        int[] maxes = new int[spectrum.Length];
        // 極小値インデックスの配列
        int[] mins = new int[spectrum.Length];
        // ピーク値インデックスの配列
        int[] peaks = new int[spectrum.Length];

        //極大値の探索
        int count = 0;
        for (int i = 0; i < spectrum.Length - EXTREMUM_RANGE; i++)
        {
            if (GetMaxNo(i, EXTREMUM_RANGE, spectrum) == GetMaxNo(i + 1, EXTREMUM_RANGE, spectrum))
            {
                int check = 0;
                for (int k = 1; k < EXTREMUM_RANGE; k++)
                {
                    if (GetMaxNo(i, EXTREMUM_RANGE, spectrum) == GetMaxNo(i + k, EXTREMUM_RANGE, spectrum))
                    {
                        check++;
                    }
                }
                if (check == EXTREMUM_RANGE - 1)
                {
                    maxes[count] = GetMaxNo(i, EXTREMUM_RANGE, spectrum);
                    count++;
                }
            }   
        }

        //極小値の探索
        mins[0] = GetMinNo(0, maxes[0], spectrum);
        for (int i = 0; i < spectrum.Length; i++)
        {
            if (maxes[i + 1] == 0) break;
            mins[i + 1] = GetMinNo(maxes[i], maxes[i + 1], spectrum);
        }

        //差分の計算
        int peakscnt = 0;
        for (int i = 0; i < spectrum.Length; i++)
        {
            if (spectrum[maxes[i]] - spectrum[mins[i]] >= EXTREMUM_THRESHOLD)
            {
                peaks[peakscnt] = maxes[i];
                peakscnt++;
            }
        }

        /*
         * ピーク周波数を返す
         */
        // ピーク周波数インデックスの配列
        float[] freqs = new float[peakscnt];
        // ピーク周波数
        float[] pitches = new float[peakscnt];

        // 各ピークの前後のスペクトルも考慮
        for (int i = 0; i < peakscnt; i++)
        {
            freqs[i] = peaks[i];

            if (peaks[i] > 0 && peaks[i] < spectrum.Length - 1)
            {
                float dL = spectrum[peaks[i] - 1] / spectrum[peaks[i]];
                float dR = spectrum[peaks[i] + 1] / spectrum[peaks[i]];

                freqs[i] += 0.5f * (dR * dR - dL * dL);
            }
            pitches[i] = freqs[i] * (AudioSettings.outputSampleRate / 2) / spectrum.Length;
            // 検出する周波数域を考慮
            if (pitches[i] >= FREQUENCY_RANGE) pitches[i] = 0;
        }
        return pitches;
    }

    /*
     * 周波数から音階に変換
     */
    public static string ConvertHertzToScale(float hertz)
    {
        // 周波数を，C2を0，（中略），C3を12とする数値に変換
        float scale = 12.0f * Mathf.Log(hertz / 65.5f) / Mathf.Log(2.0f); // C2,65.5Hz

        // 四捨五入
        int s = (int)scale;
        if (scale - s >= 0.5) s += 1;

        int smod = s % 12; // 音階
        int soct = s / 12; // オクターブ

        string value; // 音階
        if (smod == 0) value = "C";
        else if (smod == 1) value = "C#";
        else if (smod == 2) value = "D";
        else if (smod == 3) value = "D#";
        else if (smod == 4) value = "E";
        else if (smod == 5) value = "F";
        else if (smod == 6) value = "F#";
        else if (smod == 7) value = "G";
        else if (smod == 8) value = "G#";
        else if (smod == 9) value = "A";
        else if (smod == 10) value = "A#";
        else if (smod == 11) value = "B";
        else value = "EXCEPTION";
        value += soct + 2;

        return value;
    }

    /*
     * 制限時間を過ぎた場合に成功回数を初期化
     */
    public void RefreshGoodcounter()
    {
        Goodcounter = 0;
        Goodscale.fillAmount = 0;
    }

    void Start () {
        // コード取得
        Codes = GameObject.Find("Codes");

        // マイク入力
        Mic.clip = Microphone.Start(null, true, 999, 44100); // マイク名, ループするかどうか, AudioClipの秒数, サンプリングレート
        while (!(Microphone.GetPosition(null) > 0)) { } // マイクの準備ができるまで待つ

        Mic.Play();
    }
	
	void Update () {
        // FFT，スペクトル波形描画，ピーク値の算出，ピーク周波数を返す
        float[] hertz = AnalyzeSound(Mic);
        // 各ピーク周波数に対して：開始
        for (int i = 0; i < hertz.Length; i++)
        {
            if (hertz[i] == 0) break;
            // 周波数から音階に変換
            string scale = ConvertHertzToScale(hertz[i]);
            // 表示
            //Debug.Log("<color=red>PHASE:" + Phase + "</color>");
            //Debug.Log(hertz[i] + "Hz, Scale:" + scale);

            // 音階をもとにギターの各fに紐付け
            switch (scale)
            {
                // 1弦
                case "E4":
                    this.transform.Find("String1_0").gameObject.SetActive(true);
                    break;
                case "F4":
                    this.transform.Find("String1_1").gameObject.SetActive(true);
                    break;
                case "F#4":
                    this.transform.Find("String1_2").gameObject.SetActive(true);
                    break;
                case "G4":
                    this.transform.Find("String1_3").gameObject.SetActive(true);
                    break;
                case "G#4":
                    this.transform.Find("String1_4").gameObject.SetActive(true);
                    break;

                // 2弦
                case "B3":
                    this.transform.Find("String2_0").gameObject.SetActive(true);
                    this.transform.Find("String3_4").gameObject.SetActive(true);
                    break;
                case "C4":
                    this.transform.Find("String2_1").gameObject.SetActive(true);
                    break;
                case "C#4":
                    this.transform.Find("String2_2").gameObject.SetActive(true);
                    break;
                case "D4":
                    this.transform.Find("String2_3").gameObject.SetActive(true);
                    break;
                case "D#4":
                    this.transform.Find("String2_4").gameObject.SetActive(true);
                    break;

                // 3弦
                case "G3":
                    this.transform.Find("String3_0").gameObject.SetActive(true);
                    break;
                case "G#3":
                    this.transform.Find("String3_1").gameObject.SetActive(true);
                    break;
                case "A3":
                    this.transform.Find("String3_2").gameObject.SetActive(true);
                    break;
                case "A#3":
                    this.transform.Find("String3_3").gameObject.SetActive(true);
                    break;

                // 4弦
                case "D3":
                    this.transform.Find("String4_0").gameObject.SetActive(true);
                    break;
                case "D#3":
                    this.transform.Find("String4_1").gameObject.SetActive(true);
                    break;
                case "E3":
                    this.transform.Find("String4_2").gameObject.SetActive(true);
                    break;
                case "F3":
                    this.transform.Find("String4_3").gameObject.SetActive(true);
                    break;
                case "F#3":
                    this.transform.Find("String4_4").gameObject.SetActive(true);
                    break;

                // 5弦
                case "A2":
                    this.transform.Find("String5_0").gameObject.SetActive(true);
                    break;
                case "A#2":
                    this.transform.Find("String5_1").gameObject.SetActive(true);
                    break;
                case "B2":
                    this.transform.Find("String5_2").gameObject.SetActive(true);
                    break;
                case "C3":
                    this.transform.Find("String5_3").gameObject.SetActive(true);
                    break;
                case "C#3":
                    this.transform.Find("String5_4").gameObject.SetActive(true);
                    break;

                // 6弦
                case "E2":
                    this.transform.Find("String6_0").gameObject.SetActive(true);
                    break;
                case "F2":
                    this.transform.Find("String6_1").gameObject.SetActive(true);
                    break;
                case "F#2":
                    this.transform.Find("String6_2").gameObject.SetActive(true);
                    break;
                case "G2":
                    this.transform.Find("String6_3").gameObject.SetActive(true);
                    break;
                case "G#2":
                    this.transform.Find("String6_4").gameObject.SetActive(true);
                    break;

                default:
                    break;
            }
        }// 各ピーク周波数に対して：終了
        
        // 現在アクティブになっている『お手本のモデル』別に処理：開始
        switch (Codes.GetComponent<CodesManager>().getCode())
        {
            // 表示されているお手本のモデルが"A"コードのとき
            case "A": // (1)0fE4, (2)2fC#4, (3)2fA3, (4)2fE3, (5)0fA2, (6)ミュート
                // 押さえないであろう箇所をパッシブ化
                this.transform.Find("String1_4").gameObject.SetActive(false);
                this.transform.Find("String2_4").gameObject.SetActive(false);
                this.transform.Find("String3_4").gameObject.SetActive(false);
                this.transform.Find("String4_4").gameObject.SetActive(false);
                this.transform.Find("String5_4").gameObject.SetActive(false);
                this.transform.Find("String6_4").gameObject.SetActive(false);

                // 1弦：0f，E4
                if(this.transform.Find("String1_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                    this.transform.Find("String1_0").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String1_0").gameObject.activeInHierarchy) // 2fが鳴っていなくて，0fが鳴っている
                {
                    string1 = true;
                    this.transform.Find("String1_1").gameObject.SetActive(false);
                    this.transform.Find("String1_2").gameObject.SetActive(false);
                    this.transform.Find("String1_3").gameObject.SetActive(false);
                }
                else // 2fも0fも鳴っていない．すなわち，何fも鳴っていないもしくは3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "1弦をミュートしている，もしくはピッキングできていません！";
                }

                // 2弦：2f，C#4
                if (this.transform.Find("String2_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string2 = true;
                    this.transform.Find("String2_1").gameObject.SetActive(false);
                    this.transform.Find("String2_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String2_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }
                else // 2fも3fもなっていない．すなわち，何fも鳴っていない，もしくは1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "2弦をミュートしている，もしくはピッキングできていません！";
                }

                // 3弦：2f，A3
                if(this.transform.Find("String3_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string3 = true;
                    this.transform.Find("String3_1").gameObject.SetActive(false);
                    this.transform.Find("String3_3").gameObject.SetActive(false);
                }
                else // 2fが鳴っていない
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が違います！";
                }

                // 4弦：2f，E3
                if(this.transform.Find("String4_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string4 = true;
                    this.transform.Find("String4_1").gameObject.SetActive(false);
                    this.transform.Find("String4_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String4_1").gameObject.activeInHierarchy) // 2fが鳴っていなくて，1fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }
                else // 1fも2fも鳴っていない．すなわち，何fも鳴っていない，もしくは3fが鳴っている．
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "4弦をミュートしている，もしくはピッキングできていません！";
                }

                // 5弦：0f，A2
                if(this.transform.Find("String5_0").gameObject.activeInHierarchy) // 0fが鳴っている
                {
                    string5 = true;
                    this.transform.Find("String5_1").gameObject.SetActive(false);
                    this.transform.Find("String5_2").gameObject.SetActive(false);
                    this.transform.Find("String5_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String5_2").gameObject.activeInHierarchy)// 0fが鳴っていなくて，2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が違います！";
                }
                else // 0fも2fも鳴っていない．すなわち，何fも鳴っていない．もしくは，1fか3fが鳴っている．
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "5弦をミュートしている，もしくはピッキングできていません！";
                }

                // 6弦：ミュート
                if(this.transform.Find("String6_0").gameObject.activeInHierarchy || this.transform.Find("String6_1").gameObject.activeInHierarchy || this.transform.Find("String6_2").gameObject.activeInHierarchy || this.transform.Find("String6_3").gameObject.activeInHierarchy) // 何fかが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "6弦をミュートできていません！";
                }
                else // 何fも鳴っていない
                {
                    string6 = true;
                }
                break;

            // 表示されているお手本のモデルが"B"コードのとき
            case "B": // (1)2fF#4, (2)4fD#4, (3)4fB3, (4)4fF#3, (5)2fB2, (6)ミュート
                // 押さえないであろう箇所をパッシブ化
                this.transform.Find("String1_1").gameObject.SetActive(false);
                this.transform.Find("String2_1").gameObject.SetActive(false);
                this.transform.Find("String3_1").gameObject.SetActive(false);
                this.transform.Find("String4_1").gameObject.SetActive(false);
                this.transform.Find("String5_1").gameObject.SetActive(false);
                this.transform.Find("String6_1").gameObject.SetActive(false);

                // 1弦：2f，F#4
                if (this.transform.Find("String1_4").gameObject.activeInHierarchy) // 4fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "小指の位置が違います！";
                    this.transform.Find("String1_2").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String1_3").gameObject.activeInHierarchy) // 4fが鳴っていなくて，3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が違います！";
                    this.transform.Find("String1_2").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String1_2").gameObject.activeInHierarchy) // 4fも3fも鳴っていなくて，2fが鳴っている
                {
                    string1 = true;
                    this.transform.Find("String1_3").gameObject.SetActive(false);
                    this.transform.Find("String1_4").gameObject.SetActive(false);
                }
                else // 4fも3fも2fも鳴っていない．すなわち，何fも鳴っていない，もしくは0fか1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "1弦をミュートしている，もしくはピッキングできていません！";
                }

                // 2弦：4f，D#4
                if (this.transform.Find("String2_4").gameObject.activeInHierarchy) // 4fが鳴っている
                {
                    string2 = true;
                    this.transform.Find("String2_2").gameObject.SetActive(false);
                    this.transform.Find("String2_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String1_0").gameObject.activeInHierarchy) // 4fが鳴っていなくて，5f(1弦0fに相当)が鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "小指の位置が違います！";
                }
                else // 4fも5fも鳴っていない
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "小指の位置が違います！";
                }

                // 3弦：4f，B3
                if (this.transform.Find("String3_4").gameObject.activeInHierarchy) // 4fが鳴っている
                {
                    string3 = true;
                    this.transform.Find("String3_2").gameObject.SetActive(false);
                    this.transform.Find("String3_3").gameObject.SetActive(false);
                }
                else // 4fが鳴っていない
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が違います！";
                }

                // 4弦：4f，F#3
                if (this.transform.Find("String4_4").gameObject.activeInHierarchy) // 4fが鳴っている
                {
                    string4 = true;
                    this.transform.Find("String4_2").gameObject.SetActive(false);
                    this.transform.Find("String4_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String4_3").gameObject.activeInHierarchy) // 4fが鳴っていなくて，3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が違います！";
                }
                else // 3fも4fも鳴っていない
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が違います！";
                }

                // 5弦：2f，B2
                if (this.transform.Find("String5_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string5 = true;
                    this.transform.Find("String5_3").gameObject.SetActive(false);
                    this.transform.Find("String5_4").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String5_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が違います！";
                }
                else // 2fも3fも鳴っていない
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が違います！";
                }

                // 6弦：ミュート
                if (this.transform.Find("String6_0").gameObject.activeInHierarchy || this.transform.Find("String6_1").gameObject.activeInHierarchy || this.transform.Find("String6_2").gameObject.activeInHierarchy || this.transform.Find("String6_3").gameObject.activeInHierarchy || this.transform.Find("String6_4").gameObject.activeInHierarchy) // 何fかが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "6弦をミュートできていません！";
                }
                else // 何fも鳴っていない
                {
                    string6 = true;
                }
                break;

            // 表示されているお手本のモデルが"C"コードのとき
            case "C": // (1)0fE4, (2)1fC4, (3)0fG3, (4)2fE3, (5)3fC3, (6)ミュート
                // 3弦の倍音である1弦3fをパッシブ化
                this.transform.Find("String1_3").gameObject.SetActive(false);
                // 押さえないであろう箇所をパッシブ化
                //this.transform.Find("String1_2").gameObject.SetActive(false);
                this.transform.Find("String1_4").gameObject.SetActive(false);
                //this.transform.Find("String2_3").gameObject.SetActive(false);
                this.transform.Find("String2_4").gameObject.SetActive(false);
                //this.transform.Find("String3_3").gameObject.SetActive(false);
                this.transform.Find("String3_4").gameObject.SetActive(false);
                //this.transform.Find("String4_1").gameObject.SetActive(false);
                this.transform.Find("String4_4").gameObject.SetActive(false);
                this.transform.Find("String5_4").gameObject.SetActive(false);
                this.transform.Find("String6_4").gameObject.SetActive(false);

                // 1弦：0f，E4
                if (this.transform.Find("String1_1").gameObject.activeInHierarchy) // 1fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                    this.transform.Find("String1_0").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String1_0").gameObject.activeInHierarchy) // 1fが鳴っていなくて，0fが鳴っている
                {
                    string1 = true;
                    this.transform.Find("String1_1").gameObject.SetActive(false);
                    this.transform.Find("String1_2").gameObject.SetActive(false);
                }
                else // 0fも1fも鳴っていない．すなわち，何fも鳴っていないもしくは2fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "1弦をミュートしている，もしくはピッキングできていません！";
                }

                // 2弦：1f，C4
                if (this.transform.Find("String2_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                    this.transform.Find("String2_1").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String2_1").gameObject.activeInHierarchy) // 2fが鳴っていなくて，1fが鳴っている
                {
                    string2 = true;
                    this.transform.Find("String2_2").gameObject.SetActive(false);
                    this.transform.Find("String2_3").gameObject.SetActive(false);
                }
                else // 1fも2fも鳴っていない．すなわち，何fも鳴っていないもしくは0fか3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }

                // 3弦：0f，G3
                if(this.transform.Find("String3_0").gameObject.activeInHierarchy) // 0fが鳴っている
                {
                    string3 = true;
                    this.transform.Find("String3_1").gameObject.SetActive(false);
                    this.transform.Find("String3_2").gameObject.SetActive(false);
                    this.transform.Find("String3_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String3_1").gameObject.activeInHierarchy) // 0fがなっていなくて，1fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }
                else if(this.transform.Find("String3_2").gameObject.activeInHierarchy) // 0fも1fも鳴っていなくて，2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                }
                else // 0fも1fも2fも鳴っていない ．すなわち，何fもなっていないもしくは3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "3弦をミュートしている，もしくはピッキングできていません！";
                }

                // 4弦：2f，E3
                if (this.transform.Find("String4_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string4 = true;
                    this.transform.Find("String4_1").gameObject.SetActive(false);
                    this.transform.Find("String4_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String4_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }
                else // 2fも3fも鳴っていない．すなわち，何fも鳴っていないもしくは1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "4弦をミュートしている，もしくはピッキングできていません！";
                }

                // 5弦：3f，C3
                if (this.transform.Find("String5_3").gameObject.activeInHierarchy) // 3fが鳴っている
                {
                    string5 = true;
                    this.transform.Find("String5_1").gameObject.SetActive(false);
                    this.transform.Find("String5_2").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String5_2").gameObject.activeInHierarchy) // 3fが鳴っていなくて，2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指，薬指の位置が間違えています！";
                }
                else // 2fも3fも鳴っていない．もしくは，何fも鳴っていないもしくは1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "5弦をミュートしている，もしくはピッキングできていません！";
                }

                // 6弦：ミュート
                if(this.transform.Find("String6_3").gameObject.activeInHierarchy) // 3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }
                else if(this.transform.Find("String6_0").gameObject.activeInHierarchy || this.transform.Find("String6_1").gameObject.activeInHierarchy || this.transform.Find("String6_2").gameObject.activeInHierarchy) // 3f以外のいずれかの音が鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "6弦をミュートできていません！";
                }
                else // 何fも鳴っていない
                {
                    string6 = true;
                }
                break;

            // 表示されているお手本のモデルが"D"コードのとき
            case "D": // (1)2fF#4, (2)3fD4, (3)2fA3, (4)0fD3，(5)ミュート，(6)ミュート
                // 押さえないであろう箇所をパッシブ化
                this.transform.Find("String4_4").gameObject.SetActive(false);
                this.transform.Find("String5_4").gameObject.SetActive(false);
                this.transform.Find("String6_4").gameObject.SetActive(false);

                // 1弦：2f，F#4
                if (this.transform.Find("String1_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string1 = true;
                    this.transform.Find("String1_1").gameObject.SetActive(false);
                    this.transform.Find("String1_3").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String1_1").gameObject.activeInHierarchy || this.transform.Find("String1_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，1fか3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                }
                else // 1fも2fも3fも鳴っていない．すなわち，何fも鳴っていないもしくは2fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                }

                // 2弦：3f，D4
                if (this.transform.Find("String2_2").gameObject.activeInHierarchy || this.transform.Find("String2_4").gameObject.activeInHierarchy) // 2fか4fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                    this.transform.Find("String2_3").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String2_3").gameObject.activeInHierarchy) // 2fも4fも押さえていなくて，3fを押さえている
                {
                    string2 = true;
                    this.transform.Find("String2_2").gameObject.SetActive(false);
                    this.transform.Find("String2_4").gameObject.SetActive(false);
                }
                else // 2fも3fも4fも鳴っていない．すなわち，何fも押さえていない，もしくは0fか1fを押さえている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }

                // 3弦：2f，A3
                if (this.transform.Find("String3_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string3 = true;
                    this.transform.Find("String3_1").gameObject.SetActive(false);
                    this.transform.Find("String3_3").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String3_1").gameObject.activeInHierarchy || this.transform.Find("String3_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，1fか3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }
                else // 1fも2fも3fも鳴っていない．すなわち，何fも鳴っていないもしくは2fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                }

                // 4弦：0f，D3
                if (this.transform.Find("String4_0").gameObject.activeInHierarchy) // 0fが鳴っている
                {
                    string4 = true;
                    this.transform.Find("String4_1").gameObject.SetActive(false);
                    this.transform.Find("String4_2").gameObject.SetActive(false);
                    this.transform.Find("String4_3").gameObject.SetActive(false);
                    this.transform.Find("String4_4").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String4_2").gameObject.activeInHierarchy) // 0fが鳴っていなくて，2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }
                else // 0dも2fも鳴っていない．すなわち，何fも鳴っていない，もしくは1fか3fか4fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }

                // 5弦：ミュート
                if (this.transform.Find("String5_0").gameObject.activeInHierarchy || this.transform.Find("String5_1").gameObject.activeInHierarchy || this.transform.Find("String5_2").gameObject.activeInHierarchy || this.transform.Find("String5_3").gameObject.activeInHierarchy || this.transform.Find("String5_4").gameObject.activeInHierarchy) // いずれかの音が鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "5弦をミュートできていません！";
                }
                else // 何fも鳴っていない
                {
                    string5 = true;
                }

                // 6弦：ミュート
                if (this.transform.Find("String6_0").gameObject.activeInHierarchy || this.transform.Find("String6_1").gameObject.activeInHierarchy || this.transform.Find("String6_2").gameObject.activeInHierarchy || this.transform.Find("String6_3").gameObject.activeInHierarchy || this.transform.Find("String6_4").gameObject.activeInHierarchy) // 何fかが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "6弦をミュートできていません！";
                }
                else // 何fも鳴っていない
                {
                    string6 = true;
                }
                break;

            // 表示されているお手本のモデルが"E"コードのとき
            case "E": // (1)0fE4, (2)0fB3, (3)1fG#3, (4)2fE3, (5)2fB2, (6)0fE2
                // 押さえないであろう箇所をパッシブ化
                this.transform.Find("String1_3").gameObject.SetActive(false);
                this.transform.Find("String1_4").gameObject.SetActive(false);
                this.transform.Find("String2_4").gameObject.SetActive(false);
                this.transform.Find("String3_4").gameObject.SetActive(false);
                this.transform.Find("String4_4").gameObject.SetActive(false);
                this.transform.Find("String5_4").gameObject.SetActive(false);
                this.transform.Find("String6_4").gameObject.SetActive(false);

                // 1弦：0f，E4
                if (this.transform.Find("String1_0").gameObject.activeInHierarchy) // 0fが鳴っている
                {
                    string1 = true;
                }
                else // 0fが鳴っていない．すなわち，何fも鳴っていないもしくは1fか2fか3fか4fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "1弦をミュートしている，もしくはピッキングできていません！";
                }

                // 2弦：0f，B3
                if (this.transform.Find("String1_1").gameObject.activeInHierarchy) // 1fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }
                else if (this.transform.Find("String1_0").gameObject.activeInHierarchy) // 1fが鳴っていなくて，0fが鳴っている
                {
                    string2 = true;
                }
                else // 0fも1fも鳴っていない．すなわち，何fも鳴っていないもしくは2fか3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }

                // 3弦：1f，G#3
                if (this.transform.Find("String3_1").gameObject.activeInHierarchy) // 1fが鳴っている
                {
                    string3 = true;
                    this.transform.Find("String3_2").gameObject.SetActive(false);
                    this.transform.Find("String3_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String3_2").gameObject.activeInHierarchy) // 1fが鳴っていなくて，2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指，中指の位置が間違えています！";
                }

                // 4弦：2f，E3
                if (this.transform.Find("String4_3").gameObject.activeInHierarchy) // 3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                    this.transform.Find("String4_2").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String4_2").gameObject.activeInHierarchy) // 3fが鳴っていなくて，2fが鳴っている
                {
                    string4 = true;
                    this.transform.Find("String4_1").gameObject.SetActive(false);
                    this.transform.Find("String4_3").gameObject.SetActive(false);
                }
                else // 2fも3fも鳴っていない．すなわち，何fも鳴っていないもしくは0fか1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }

                // 5弦：2f，B2
                if (this.transform.Find("String5_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string5 = true;
                    this.transform.Find("String5_1").gameObject.SetActive(false);
                    this.transform.Find("String5_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String5_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                }
                else // 2fも3fも鳴っていない．すなわち，何fも鳴っていないもしくは0fか1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                }

                // 6弦：0f，E2
                if (this.transform.Find("String6_0").gameObject.activeInHierarchy) // 0fが鳴っている
                {
                    string6 = true;
                    this.transform.Find("String6_1").gameObject.SetActive(false);
                    this.transform.Find("String6_2").gameObject.SetActive(false);
                    this.transform.Find("String6_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String6_2").gameObject.activeInHierarchy) // 0fが鳴っていなくて，2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                }
                else // 0fも2fも鳴っていない．すなわち，何fも鳴っていないもしくは1fか3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "6弦をミュートしている，もしくはピッキングできていません！";
                }
                break;

            // 表示されているお手本のモデルが"F"コードのとき
            case "F": // (1)1fF4, (2)1fC4, (3)2fA3, (4)3fF3, (5)3fC3, (6)1fF2
                // 押さえないであろう箇所をパッシブ化
                this.transform.Find("String1_4").gameObject.SetActive(false);
                this.transform.Find("String2_4").gameObject.SetActive(false);
                this.transform.Find("String3_4").gameObject.SetActive(false);
                this.transform.Find("String4_4").gameObject.SetActive(false);
                this.transform.Find("String5_4").gameObject.SetActive(false);
                this.transform.Find("String6_4").gameObject.SetActive(false);

                // 1弦：1f，F4
                if (this.transform.Find("String1_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                    this.transform.Find("String1_1").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String1_1").gameObject.activeInHierarchy) // 2fが鳴っていなくて，1fが鳴っている
                {
                    string1 = true;
                    this.transform.Find("String1_2").gameObject.SetActive(false);
                    this.transform.Find("String1_3").gameObject.SetActive(false);
                }
                else // 1fも2fも鳴っていない．すなわち，何fも鳴っていないもしくは0fか3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "1弦をミュートしている，もしくはピッキングできていません！";
                }

                // 2弦：1f，C4
                if (this.transform.Find("String2_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                    this.transform.Find("String2_1").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String2_1").gameObject.activeInHierarchy) // 2fが鳴っていなくて，1fが鳴っている
                {
                    string2 = true;
                    this.transform.Find("String2_2").gameObject.SetActive(false);
                    this.transform.Find("String2_3").gameObject.SetActive(false);
                }
                else // 1fも2fも鳴っていない．すなわち，何fも鳴っていないもしくは0fか3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "2弦をミュートしている，もしくはピッキングできていません！";
                }

                // 3弦：2f，A3
                if (this.transform.Find("String3_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string3 = true;
                    this.transform.Find("String3_1").gameObject.SetActive(false);
                    this.transform.Find("String3_3").gameObject.SetActive(false);
                }
                else if(this.transform.Find("String3_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "小指の位置が間違えています！";
                }
                else // 2fも3fも鳴っていない．すなわち，何fも鳴っていないもしくは0fか1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！！";
                }

                // 4弦：3f，F3
                if (this.transform.Find("String4_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                    this.transform.Find("String4_3").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String4_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，3fが鳴っている
                {
                    string4 = true;
                    this.transform.Find("String4_1").gameObject.SetActive(false);
                    this.transform.Find("String4_2").gameObject.SetActive(false);
                }
                else // 2fも3fも鳴っていない．すなわち，何fも鳴っていない．もしくは，0fか1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "小指の位置が間違えています！";
                }

                // 5弦：3f，C3
                if (this.transform.Find("String5_3").gameObject.activeInHierarchy) // 3fが鳴っている
                {
                    string5 = true;
                    this.transform.Find("String5_1").gameObject.SetActive(false);
                    this.transform.Find("String5_2").gameObject.SetActive(false);
                }
                else // 3fが鳴っていない．すなわち，何fも鳴っていない．もしくは，0fか1fか2fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }

                // 6弦：1f，F2
                if (this.transform.Find("String6_1").gameObject.activeInHierarchy) // 1fが鳴っている
                {
                    string6 = true;
                    this.transform.Find("String6_2").gameObject.SetActive(false);
                    this.transform.Find("String6_3").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String6_3").gameObject.activeInHierarchy) // 1fが鳴っていなくて，3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }
                else // 1fも3fも鳴っていない．すなわち，何fも鳴っていないもしくは0fか2fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }
                break;

            // 表示されているお手本のモデルが"G"コードのとき
            case "G": // (1)3fG4, (2)0fB3, (3)0fG3, (4)0fD3, (5)2fB2, (6)3fG2
                // 押さえないであろう箇所をパッシブ化
                this.transform.Find("String1_4").gameObject.SetActive(false);
                this.transform.Find("String2_3").gameObject.SetActive(false);
                this.transform.Find("String2_4").gameObject.SetActive(false);
                this.transform.Find("String3_4").gameObject.SetActive(false);
                this.transform.Find("String4_4").gameObject.SetActive(false);
                this.transform.Find("String5_4").gameObject.SetActive(false);

                // 1弦：3f，G4
                if (this.transform.Find("String1_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                    this.transform.Find("String1_3").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String1_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，3fが鳴っている
                {
                    string1 = true;
                    this.transform.Find("String1_1").gameObject.SetActive(false);
                    this.transform.Find("String1_2").gameObject.SetActive(false);
                }
                else // 2fも3fも鳴っていない．すなわち，何fも鳴っていないもしくは0fか1fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "薬指の位置が間違えています！";
                }

                // 2弦：0f，B3
                if (this.transform.Find("String2_0").gameObject.activeInHierarchy) // 0fが鳴っている
                {
                    string2 = true;
                }
                else // 0fが鳴っていない．すなわち，何fもなっていないもしくは1fか2fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "2弦をミュートしている，もしくはピッキングできていません！";
                }

                // 3弦：0f，G3
                if (this.transform.Find("String3_0").gameObject.activeInHierarchy) // 0fが鳴っている
                {
                    string3 = true;
                }
                else // 0fが鳴っていない．すなわち，何fもなっていないもしくは1fか2fか3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "3弦をミュートしている，もしくはピッキングできていません！";
                }

                // 4弦：0f，D3
                string4 = true;
                if (this.transform.Find("String4_0").gameObject.activeInHierarchy) // 0fが鳴っている
                {
                    this.transform.Find("String4_1").gameObject.SetActive(false);
                    this.transform.Find("String4_2").gameObject.SetActive(false);
                    this.transform.Find("String4_3").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String4_2").gameObject.activeInHierarchy) // 0fが鳴っていなくて，2fが鳴っている
                {
                    string4 = false;
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }
                else // 0fも2fも鳴っていない．すなわち，何fもなっていないもしくは1fか3fが鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "4弦をミュートしている，もしくはピッキングできていません！";
                }

                // 5弦：2f，B2
                if (this.transform.Find("String5_2").gameObject.activeInHierarchy) // 2fが鳴っている
                {
                    string5 = true;
                    this.transform.Find("String5_1").gameObject.SetActive(false);
                    this.transform.Find("String5_3").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String5_1").gameObject.activeInHierarchy || this.transform.Find("String5_3").gameObject.activeInHierarchy) // 2fが鳴っていなくて，1fか3fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "人差指の位置が間違えています！";
                }
                else // 1fも2fも3fも鳴っていない．すなわち，何fもなっていないもしくは0f鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "4弦をミュートしている，もしくはピッキングできていません！";
                }

                // 6弦：3f，G2
                if (this.transform.Find("String6_3").gameObject.activeInHierarchy) // 3fが鳴っている
                {
                    string6 = true;
                    this.transform.Find("String6_1").gameObject.SetActive(false);
                    this.transform.Find("String6_2").gameObject.SetActive(false);
                    this.transform.Find("String6_4").gameObject.SetActive(false);
                }
                else if (this.transform.Find("String6_2").gameObject.activeInHierarchy || this.transform.Find("String6_4").gameObject.activeInHierarchy) // 3fが鳴っていなくて，2fか4fが鳴っている
                {
                    this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "中指の位置が間違えています！";
                }
                else // 2fも3fも4f鳴っていない．すなわち，何fもなっていないもしくは0fか1f鳴っている
                {
                    // this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "4弦をミュートしている，もしくはピッキングできていません！";
                }
                break;

            default:
                break;
        }// 現在アクティブになっている『お手本のモデル』別に処理：終了

        // 1～6弦がすべて正しく押さえ/ミュートされているとき
        if (string1 && string2 && string3 && string4 && string5 && string6)
        {
            // コメントと赤丸の表示
            this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "GOOD!";
            this.transform.Find("Good").gameObject.SetActive(true);
            // 成功回数の更新
            ++Goodcounter;
            Goodscale.fillAmount += 1.0f / MAX_GOODCOUNTER;
        }

        // 成功回数が一定数を超えたとき
        if(Goodcounter >= MAX_GOODCOUNTER)
        {
            // お手本のモデルの変更
            Codes.GetComponent<CodesManager>().changeCode();
            // 成功回数の初期化
            Goodcounter = 0;
            Goodscale.fillAmount = 0;
        }

        // リフレッシュ
        if (++Phase % CYCLE == 0)
        {
            // 押さえているポジション，コメントの初期化
            foreach (Transform child in this.transform)
            {
                child.gameObject.SetActive(false);
            }
            // 各弦の正しく抑えているかの判定変数の初期化
            string1 = false;
            string2 = false;
            string3 = false;
            string4 = false;
            string5 = false;
            string6 = false;
            // コメントの初期化
            this.transform.Find("Comment").gameObject.SetActive(true);
            this.transform.Find("Comment").gameObject.GetComponent<TextMesh>().text = "";
            this.transform.Find("Canvas").gameObject.SetActive(true);
        }
    }
}