using System;
using UnityEditor;
using UnityEngine;
using TheLastArk.Skills;

/// <summary>Isolated particle preview owned by the existing character editor.</summary>
internal sealed class SkillVfxPreview : IDisposable
{
    public SkillVfxSettings Settings { get; private set; }
    private readonly Action repaint;
    private PreviewRenderUtility preview;
    private GameObject instance;
    private ParticleSystem[] particles;
    private double previousTime;
    private float elapsed, zoom = 5f, speed = 1f;
    private bool playing = true, loop = true;
    private Color background = new Color(.08f, .12f, .18f);

    public SkillVfxPreview(Action repaint)
    {
        this.repaint = repaint;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public void Load(SkillVfxSettings settings)
    {
        Clear();
        Settings = settings;
        if (settings?.prefab == null) return;
        preview = new PreviewRenderUtility();
        preview.camera.orthographic = true;
        preview.camera.nearClipPlane = .01f;
        preview.camera.farClipPlane = 1000f;
        preview.camera.transform.position = new Vector3(0, 0, -100);
        preview.camera.transform.rotation = Quaternion.identity;
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.lights[0].intensity = 1f;
        instance = preview.InstantiatePrefabInScene(settings.prefab);
        // The preview simulates particles only, never gameplay scripts or sound.
        foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (var source in instance.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
        particles = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.useAutoRandomSeed = false;
            ps.randomSeed = 1;
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        Restart();
    }

    private void Restart()
    {
        elapsed = 0f;
        previousTime = EditorApplication.timeSinceStartup;
        playing = true;
        Simulate();
    }

    private void Simulate()
    {
        if (instance == null || Settings == null) return;
        instance.transform.position = new Vector3(Settings.offset.x / 140f, Settings.offset.y / 140f, 0f);
        instance.transform.rotation = Quaternion.Euler(Settings.rotation);
        instance.transform.localScale = Settings.prefab.transform.localScale * Mathf.Max(.01f, Settings.scale);
        foreach (var ps in particles)
            if (ps != null && ps.gameObject.activeInHierarchy) ps.Simulate(elapsed, false, true, true);
    }

    private void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float delta = Mathf.Min(.1f, (float)(now - previousTime));
        previousTime = now;
        if (preview == null || Settings?.prefab == null || !playing) return;
        elapsed += delta * speed;
        if (elapsed >= Mathf.Max(.1f, Settings.lifetime))
        {
            if (loop) elapsed = 0f;
            else { elapsed = Settings.lifetime; playing = false; }
        }
        Simulate();
        repaint();
    }

    public void Draw()
    {
        if (preview == null) return;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(playing ? "일시정지" : "재생")) playing = !playing;
        if (GUILayout.Button("처음부터")) Restart();
        loop = GUILayout.Toggle(loop, "반복");
        EditorGUILayout.EndHorizontal();
        speed = EditorGUILayout.Slider("미리보기 속도", speed, .1f, 2f);
        zoom = EditorGUILayout.Slider("보이는 범위", zoom, .25f, 15f);
        background = EditorGUILayout.ColorField("미리보기 배경", background);
        var rect = GUILayoutUtility.GetRect(100, 240, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            preview.BeginPreview(rect, GUIStyle.none);
            preview.camera.backgroundColor = background;
            preview.camera.orthographicSize = zoom;
            preview.Render(true);
            GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
        }
        EditorGUILayout.LabelField($"{elapsed:0.00} / {Settings.lifetime:0.00}초 · 파티클 {particles.Length}개", EditorStyles.miniLabel);
        EditorGUILayout.HelpBox("파티클·원본 재질 미리보기입니다. 스크립트 이동·발사체·VFX Graph와 실제 화면 크기/가림은 전투 디버그에서 확인하세요.", MessageType.None);
        if (GUILayout.Button("미리보기 닫기")) Clear();
    }

    private void OnPlayModeChanged(PlayModeStateChange state) => Clear();
    private void Clear()
    {
        if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
        instance = null;
        preview?.Cleanup();
        preview = null;
        Settings = null;
    }
    public void Dispose()
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Clear();
    }
}
