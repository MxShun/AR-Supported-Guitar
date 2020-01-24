using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class MirrorManager : MonoBehaviour
{
    /*
     * OpenCVによるギターネックトラッキング
     * C++で記述したdllをNative Pluginとして使用
     */
    [DllImport("CppPlugin")] private static extern IntPtr CP_Get(int CamIndex);
    [DllImport("CppPlugin")] private static extern void   CP_Release(IntPtr cam);
    [DllImport("CppPlugin")] private static extern void   CP_Set(IntPtr cam, IntPtr p_tex_);
    [DllImport("CppPlugin")] private static extern IntPtr CP_RenderEvent();

    /*
     * カメラ用変数
     */
    public int CamIndex = 0;          // カメラ参照番号
    private IntPtr cam = IntPtr.Zero; // カメラインスタンス
    /*
     * テクスチャ貼付用変数
     */
    private Texture2D p_tex;    // Planeに貼付するテクスチャ
    public int P_Width = 640;   // p_texの横幅
    public int P_Height = 360;  // p_texの縦幅

    void Start ()
    {
        /*
         * カメラを起動
         */
        cam = CP_Get(CamIndex);

        /*
         * テクスチャの設定・貼付
         */
        p_tex = new Texture2D(P_Width, P_Height, TextureFormat.ARGB32, false);
        CP_Set(cam, p_tex.GetNativeTexturePtr());
        GetComponent<Renderer>().material.mainTexture = p_tex;

        /*
         * フレームイベントの開始
         */
        StartCoroutine(OnRender());
    }

	void Update ()
    {
    }

    void OnApplicationQuit()
    {
        /*
         * メモリリリース
         */
        CP_Release(cam);
    }

    /*
     * フレームイベント
     */
    IEnumerator OnRender()
    {
        for (; ; )
        {
            yield return new WaitForEndOfFrame();
            GL.IssuePluginEvent(CP_RenderEvent(), 0); // レンダリングスレッドで実行
        }
    }
}