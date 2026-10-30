using UnityEngine;

// Keyboard + on-screen touch buttons, read once per frame by Game.
public static class GameInput
{
    public static float Move;
    public static bool Slash, Act, Restart, Yes, No, Confirm;
    public static bool TouchMode;

    public static Rect LeftBtn, RightBtn, SlashBtn, ActBtn;
    static bool prevSlashT, prevActT;

    public static void Update()
    {
        float s = Mathf.Min(Screen.width, Screen.height) * 0.16f;
        float pad = s * 0.35f;
        LeftBtn = new Rect(pad, Screen.height - s - pad, s, s);
        RightBtn = new Rect(pad * 2 + s, Screen.height - s - pad, s, s);
        ActBtn = new Rect(Screen.width - (s + pad) * 2, Screen.height - s - pad, s, s);
        SlashBtn = new Rect(Screen.width - s - pad, Screen.height - s - pad * 1.6f, s, s);

        bool l = false, r = false, sl = false, ac = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            TouchMode = true;
            var p = Input.GetTouch(i).position;
            p.y = Screen.height - p.y; // GUI space
            if (LeftBtn.Contains(p)) l = true;
            if (RightBtn.Contains(p)) r = true;
            if (SlashBtn.Contains(p)) sl = true;
            if (ActBtn.Contains(p)) ac = true;
        }

        float k = 0f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || l) k -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || r) k += 1f;
        Move = k;

        Slash = Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J) || (sl && !prevSlashT);
        Act = Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.K) || (ac && !prevActT);
        prevSlashT = sl; prevActT = ac;

        Restart = Input.GetKeyDown(KeyCode.R);
        Yes = Input.GetKeyDown(KeyCode.Y);
        No = Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape);
        Confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z);
    }
}
