using System.Collections;
using BrothersBlock;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class LocalSessionTests
{
    [UnityTest]
    public IEnumerator HostSpawnsPlayerTouchMovesAndRoomCanReopen()
    {
        yield return SceneManager.LoadSceneAsync("Neighbourhood", LoadSceneMode.Single);
        yield return null;
        LanSession session = LanSession.Instance;
        Assert.IsNotNull(session, "Scene must contain the LAN session.");
        Assert.IsNotNull(GameHud.Instance, "Touch UI must initialize.");
        Assert.IsNotNull(Object.FindAnyObjectByType<Neighbourhood>());

        session.Host();
        float timeout = Time.realtimeSinceStartup + 8f;
        while (!session.Playing && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.IsTrue(session.Playing, session.Message);
        Assert.IsNotNull(PlayerMotor.Local, "Hosting must spawn the local player.");
        Assert.AreEqual(1, session.Players);
        yield return new WaitForSeconds(.3f);

        MoveStick stick = Object.FindAnyObjectByType<MoveStick>();
        Assert.IsNotNull(stick);
        Canvas.ForceUpdateCanvases();
        Vector3 start = PlayerMotor.Local.transform.position;
        Vector3 target = stick.transform.TransformPoint(new Vector3(60, 0, 0));
        var pointer = new PointerEventData(EventSystem.current)
        {
            pointerId = 17,
            position = RectTransformUtility.WorldToScreenPoint(null, target)
        };
        stick.OnPointerDown(pointer);
        Assert.Greater(stick.Value.x, .9f, "Touch stick should produce rightward input.");
        yield return new WaitForSeconds(.35f);
        stick.OnPointerUp(pointer);
        Assert.Greater(PlayerMotor.Local.transform.position.x - start.x, .6f, "Owner movement should respond to touch input.");
        Assert.AreEqual(Vector2.zero, stick.Value, "Releasing a touch must stop movement.");

        session.Leave();
        timeout = Time.realtimeSinceStartup + 5f;
        while ((session.Playing || session.Manager.ShutdownInProgress) && Time.realtimeSinceStartup < timeout) yield return null;
        yield return null;
        Assert.IsFalse(session.Playing);
        Assert.IsNull(PlayerMotor.Local);
        session.Host();
        timeout = Time.realtimeSinceStartup + 5f;
        while (!session.Playing && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.IsTrue(session.Playing, "A room should reopen after shutdown: " + session.Message);
        session.Leave();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator CloseSession()
    {
        if (LanSession.Instance != null) LanSession.Instance.Leave();
        yield return null;
    }
}
