using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using System;
using UniRx;
using UniRx.Triggers;
using DG.Tweening;
using TMPro;
using UnityEngine.SceneManagement;

public abstract class SelectTool
{
    public string name;
    public bool is_random;
    public float bar_speed;
    public SelectTool(string name, bool is_random, float bar_speed)
    {
        name = this.name;
        is_random = this.is_random;
        bar_speed = this.bar_speed;
    }
    public abstract float CalculateJumpResult(float x);
}
public class ROCKET : SelectTool
{
    public ROCKET(): base("ROCKET", false, 0.01f)
    {
        base.name = "ROCKET";
        base.is_random = false;
        base.bar_speed = 0.03f;
    }
    public override float CalculateJumpResult(float x){
        return MathF.Exp(MathF.Floor(x))/2;
    }
}

public class TRAMPOLINE : SelectTool
{
    public TRAMPOLINE(): base("TRAMPOLINE", false, 0.001f)
    {
        base.name = "TRAMPOLINE";
        base.is_random = false;
        base.bar_speed = 0.01f;
    }
    public override float CalculateJumpResult(float x){
        return 300*x;
    }
}

public class CANNON : SelectTool
{
    public CANNON(): base("CANNON", true, 0.001f)
    {
        base.name = "CANNON";
        base.is_random = true;
        base.bar_speed = 0.04f;
    }
    public override float CalculateJumpResult(float x){
        return 450*x;
    }
}

public class FlyGameManager : MonoBehaviour
{
    public enum GameStatus
    {
        SELECT_TIME,
        MAIN,
        ANIMATION,
        GAMEOVER
    }
    private GameStatus current_game_status;
    private SelectTool current_select_tool;
    
    [SerializeField]
    GameObject select_tool_dialog;

    [SerializeField]
    Slider jumping_bar;
    float value = 0.0f;

    [SerializeField]
    GameObject penguin;

    [SerializeField]
    List<GameObject> tools;

    [SerializeField]
    GameObject bg;

    [SerializeField]
    GameObject score;

    [SerializeField]
    GameObject result_score;

    [SerializeField]
    List<GameObject> fall_positions;

    [SerializeField]
    GameObject mokumoku;

    private float first_bg_y;
    private float result_dis = 0.0f;

    void Start()
    {
        jumping_bar.gameObject.transform.parent.gameObject.SetActive(true);
        tools[0].transform.parent.gameObject.SetActive(true);
        select_tool_dialog.SetActive(true);
        penguin.SetActive(false);
        mokumoku.SetActive(false);
        score.gameObject.transform.parent.gameObject.SetActive(false);
        result_score.gameObject.transform.parent.gameObject.SetActive(false);
        first_bg_y = bg.transform.localPosition.y;
        AudioManager.Instance.PlayBGM("ukiukilalala");
        current_game_status = GameStatus.SELECT_TIME;
        for (int i=0;i<tools.Count; i++) 
        {
            tools[i].SetActive(false);
        }

        this.UpdateAsObservable().Where( _ => current_game_status == GameStatus.MAIN).Subscribe(_ => {
            value += current_select_tool.bar_speed;
            jumping_bar.value = value % 1.0f;
        });

        this.UpdateAsObservable().Where( _ => current_game_status == GameStatus.ANIMATION).Subscribe(_ => {
            score.GetComponent<TextMeshProUGUI>().text = (-bg.transform.localPosition.y + first_bg_y).ToString("0.00") + "m";
        });
    }

    public void OnClickSelectButton(string tool)
    {
        switch (tool)
        {
            case "ROCKET":
                current_select_tool = new ROCKET();
                penguin.SetActive(true);
                tools[0].SetActive(true);
                break;
            case "TRAMPOLINE":
                current_select_tool = new TRAMPOLINE();
                penguin.SetActive(true);
                tools[1].SetActive(true);
                break;
            case "CANNON":
                current_select_tool = new CANNON();
                penguin.SetActive(false);
                tools[2].SetActive(true);
                break;
        }
        AudioManager.Instance.PlaySE("Button");
        select_tool_dialog.SetActive(false);
        current_game_status = GameStatus.MAIN;
    }

    public void OnClickJumpingBar()
    {
        if ( current_game_status != GameStatus.MAIN) return;

        float jumping_value = jumping_bar.value;
        float result = current_select_tool.CalculateJumpResult(jumping_value * 10);
        current_game_status = GameStatus.ANIMATION;
        StartCoroutine(PlayAnimation(result));
    }

    IEnumerator PlayAnimation(float dis)
    {
        score.gameObject.transform.parent.gameObject.SetActive(true);
        jumping_bar.gameObject.transform.parent.gameObject.SetActive(false);
        bool is_explosion = false;
        if (current_select_tool.name == "CANNON") 
        {
            int rand = UnityEngine.Random.Range(0, 10);
            if (rand < 5) is_explosion = true;

            if(!is_explosion)
            {
                penguin.SetActive(true);
                penguin.GetComponent<Animator>().Play("roll");
                penguin.GetComponent<Animator>().speed = 3.0f;
            }
        }
        else
        {
            penguin.GetComponent<Animator>().enabled = false;
        }
        if (is_explosion && current_select_tool.name == "CANNON")
        {
            AudioManager.Instance.PlaySE("short_bomb");
            yield return new WaitForSeconds(1);
            mokumoku.SetActive(true);
            yield return new WaitForSeconds(2);

            result_score.gameObject.transform.parent.gameObject.SetActive(true);
            result_score.GetComponent<TextMeshProUGUI>().text = "しっぱいしたよ";
            result_dis = 0.0f;
        }
        else
        {
            switch (current_select_tool.name)
            {
                case "ROCKET":
                    AudioManager.Instance.PlaySE("Jet");
                    break;
                case "TRAMPOLINE":
                    AudioManager.Instance.PlaySE("Jump");
                    break;
                case "CANNON":
                    AudioManager.Instance.PlaySE("Cannon");
                    break;
            }

            float bg_x = bg.transform.localPosition.x;
            float bg_y = bg.transform.localPosition.y;

            bg.transform.DOLocalMove(new Vector3(bg_x, bg_y - dis, 0f), 3f);
            yield return new WaitForSeconds(3);

            result_score.gameObject.transform.parent.gameObject.SetActive(true);
            result_score.GetComponent<TextMeshProUGUI>().text = dis.ToString("0.00") + "m とべたよ！";
            score.gameObject.transform.parent.gameObject.SetActive(false);
            penguin.GetComponent<Animator>().enabled = true;
            penguin.GetComponent<Animator>().Play("roll");

            int rand_index = UnityEngine.Random.Range(0, fall_positions.Count);
            Vector3 rand_pos = fall_positions[rand_index].transform.localPosition;
            penguin.transform.DOLocalMove(new Vector3(rand_pos.x, rand_pos.y, 0f), 3f);
            result_dis = dis;
        }
        current_game_status = GameStatus.GAMEOVER;
    }

    public void OnClickTweetButton()
    {
        string message = "";
        if (result_dis == 0.0f)
        {
            message = "とぶのにしっぱいしたよ。。。";
        }
        else
        {
            message = result_dis.ToString("0.00")+"m とべたよ！！";
        }
        naichilab.UnityRoomTweet.Tweet ("fly_penguin", message, "ペンギンだって空をとびたい", "unityroom", "unity1week");
    }
    public void OnClickRankingButton()
    {
        naichilab.RankingLoader.Instance.SendScoreAndShowRanking (result_dis);
    }

    public void OnClickRestartButton()
    {
        // 現在のSceneを取得
        Scene loadScene = SceneManager.GetActiveScene();
        // 現在のシーンを再読み込みする
        SceneManager.LoadScene(loadScene.name);
    }
}
