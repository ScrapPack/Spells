using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for ArenaLayoutData and ArenaPieceData configuration.
/// </summary>
[TestFixture]
public class ArenaLayoutTests
{
    [Test]
    public void ArenaPieceData_DefaultsToplatform()
    {
        var data = ScriptableObject.CreateInstance<ArenaPieceData>();
        Assert.AreEqual(ArenaPieceData.PieceType.Platform, data.pieceType);
        Assert.AreEqual(new Vector2(6f, 1f), data.size);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void ArenaPlacement_StoresPositionAndRotation()
    {
        var placement = new ArenaPlacement();
        placement.position = new Vector2(10f, 5f);
        placement.rotation = 45f;

        Assert.AreEqual(10f, placement.position.x, 0.01f);
        Assert.AreEqual(5f, placement.position.y, 0.01f);
        Assert.AreEqual(45f, placement.rotation, 0.01f);
    }

    [Test]
    public void ArenaLayoutData_GetSpawnPoints_FiltersCorrectly()
    {
        var layout = ScriptableObject.CreateInstance<ArenaLayoutData>();

        var playerPiece = ScriptableObject.CreateInstance<ArenaPieceData>();
        playerPiece.pieceType = ArenaPieceData.PieceType.SpawnPoint;
        playerPiece.spawnType = ArenaPieceData.SpawnPointType.Player;

        var monsterPiece = ScriptableObject.CreateInstance<ArenaPieceData>();
        monsterPiece.pieceType = ArenaPieceData.PieceType.SpawnPoint;
        monsterPiece.spawnType = ArenaPieceData.SpawnPointType.Monster;

        var platformPiece = ScriptableObject.CreateInstance<ArenaPieceData>();
        platformPiece.pieceType = ArenaPieceData.PieceType.Platform;

        layout.pieces = new ArenaPlacement[]
        {
            new ArenaPlacement { piece = playerPiece, position = new Vector2(0, 0) },
            new ArenaPlacement { piece = monsterPiece, position = new Vector2(5, 0) },
            new ArenaPlacement { piece = platformPiece, position = new Vector2(10, 0) },
            new ArenaPlacement { piece = playerPiece, position = new Vector2(15, 0) },
        };

        var playerSpawns = layout.GetSpawnPoints(ArenaPieceData.SpawnPointType.Player);
        var monsterSpawns = layout.GetSpawnPoints(ArenaPieceData.SpawnPointType.Monster);
        var chestSpawns = layout.GetSpawnPoints(ArenaPieceData.SpawnPointType.Chest);

        Assert.AreEqual(2, playerSpawns.Length);
        Assert.AreEqual(1, monsterSpawns.Length);
        Assert.AreEqual(0, chestSpawns.Length);

        Object.DestroyImmediate(playerPiece);
        Object.DestroyImmediate(monsterPiece);
        Object.DestroyImmediate(platformPiece);
        Object.DestroyImmediate(layout);
    }

    [Test]
    public void ArenaLayoutData_GetSpawnPoints_NullPieces_ReturnsEmpty()
    {
        var layout = ScriptableObject.CreateInstance<ArenaLayoutData>();
        layout.pieces = null;

        var spawns = layout.GetSpawnPoints(ArenaPieceData.SpawnPointType.Player);
        Assert.AreEqual(0, spawns.Length);

        Object.DestroyImmediate(layout);
    }

    [Test]
    public void ArenaLayoutData_DefaultBounds()
    {
        var layout = ScriptableObject.CreateInstance<ArenaLayoutData>();
        Assert.AreEqual(42f, layout.arenaBounds.x, 0.01f);
        Assert.AreEqual(25f, layout.arenaBounds.y, 0.01f);
        Object.DestroyImmediate(layout);
    }
}
