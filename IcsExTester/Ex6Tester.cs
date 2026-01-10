using System;
using System.Collections.Generic;
using System.Text;

namespace IcsExTester
{
    internal class Ex6Tester : ITester
    {
        const int MIN_ACTIONS = 1;
        const int MAX_ACTIONS = 200;
        const int MAX_ROOMS = 40;
        const int NAME_MIN = 1;
        const int NAME_MAX = 40;
        const int MAX_MONSTER_HP = 30;
        const int MAX_MONSTER_ATTACK = 8;
        const int MAX_ITEM_VALUE = 20;

        const double ADD_MONSTER_PROB = 0.6;
        const double ADD_ITEM_PROB = 0.6;

        Random rng = new Random();

        Dictionary<int, (int x, int y)> roomPositions = new();
        Dictionary<(int x, int y), int> posToRoom = new();
        HashSet<(int x, int y)> occupied = new();

        Dictionary<int, bool> roomHasMonster = new();
        Dictionary<int, bool> roomHasItem = new();

        int currentRoom = 0;
        List<int> defeatedMonstersRooms = new();
        List<int> bagItemRooms = new();

        public Test GenerateRandomTest()
        {
            StringBuilder sb = new StringBuilder();
            ResetState();

            int actions = rng.Next(MIN_ACTIONS, MAX_ACTIONS + 1);
            int roomCount = 0;
            bool playerInit = false;

            for (int i = 0; i < actions; i++)
            {
                int menu = rng.Next(1, 4); // 1-3
                switch (menu)
                {
                    case 1:
                        if (roomCount <= MAX_ROOMS)
                        {
                            ITester.AppendLine(sb, "1", nameof(GenerateRandomTest));
                            if (GenerateAddRoom(sb, roomCount))
                                roomCount++;
                        }
                        else
                            i--;
                        break;
                    case 2:
                        ITester.AppendLine(sb, "2", nameof(GenerateRandomTest));
                        if (roomCount > 0)
                            playerInit = true;
                        break;
                    case 3:
                        ITester.AppendLine(sb, "3", nameof(GenerateRandomTest));
                        if (playerInit && roomCount > 0)
                            GeneratePlay(sb, roomCount);
                        break;
                }
            }

            ITester.AppendLine(sb, "4", nameof(GenerateRandomTest)); // Exit
            string informativeInput = sb.ToString().Replace("\r", "");

            return new Test(informativeInput);
        }

        void ResetState()
        {
            roomPositions.Clear();
            posToRoom.Clear();
            occupied.Clear();
            roomHasMonster.Clear();
            roomHasItem.Clear();
            defeatedMonstersRooms.Clear();
            bagItemRooms.Clear();
            currentRoom = 0;
        }

        bool GenerateAddRoom(StringBuilder sb, int roomIndex)
        {
            if (roomIndex == 0)
            {
                roomPositions[0] = (0, 0);
                posToRoom[(0, 0)] = 0;
                occupied.Add((0, 0));

                roomHasMonster[0] = false;
                roomHasItem[0] = false;

                GenerateMonsterAndItem(sb, 0);
                return true;
            }

            int baseRoom = rng.Next(0, roomIndex);
            int direction = rng.Next(0, 4);

            ITester.AppendLine(sb, baseRoom.ToString(), nameof(GenerateAddRoom));
            ITester.AppendLine(sb, direction.ToString(), nameof(GenerateAddRoom));

            var (bx, by) = roomPositions[baseRoom];
            (int dx, int dy) = direction switch
            {
                0 => (0, -1),
                1 => (0, 1),
                2 => (-1, 0),
                3 => (1, 0),
                _ => (0, 0)
            };
            var target = (bx + dx, by + dy);

            if (occupied.Contains(target))
                return false;

            roomPositions[roomIndex] = target;
            posToRoom[target] = roomIndex;
            occupied.Add(target);

            roomHasMonster[roomIndex] = false;
            roomHasItem[roomIndex] = false;

            GenerateMonsterAndItem(sb, roomIndex);
            return true;
        }

        void GenerateMonsterAndItem(StringBuilder sb, int roomIndex)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (rng.NextDouble() < ADD_MONSTER_PROB)
            {
                roomHasMonster[roomIndex] = true;
                ITester.AppendLine(sb, "1", nameof(GenerateMonsterAndItem));
                ITester.AppendLine(sb, ITester.RandomWord(rng, chars, NAME_MIN, NAME_MAX), nameof(GenerateMonsterAndItem));
                ITester.AppendLine(sb, rng.Next(0, 5).ToString(), nameof(GenerateMonsterAndItem));
                ITester.AppendLine(sb, rng.Next(1, MAX_MONSTER_HP + 1).ToString(), nameof(GenerateMonsterAndItem));
                ITester.AppendLine(sb, rng.Next(1, MAX_MONSTER_ATTACK + 1).ToString(), nameof(GenerateMonsterAndItem));
            }
            else ITester.AppendLine(sb, "0", nameof(GenerateMonsterAndItem));

            if (rng.NextDouble() < ADD_ITEM_PROB)
            {
                roomHasItem[roomIndex] = true;
                ITester.AppendLine(sb, "1", nameof(GenerateMonsterAndItem));
                ITester.AppendLine(sb, ITester.RandomWord(rng, chars, NAME_MIN, NAME_MAX), nameof(GenerateMonsterAndItem));
                ITester.AppendLine(sb, rng.Next(0, 2).ToString(), nameof(GenerateMonsterAndItem));
                ITester.AppendLine(sb, rng.Next(1, MAX_ITEM_VALUE + 1).ToString(), nameof(GenerateMonsterAndItem));
            }
            else ITester.AppendLine(sb, "0", nameof(GenerateMonsterAndItem));
        }

        void GeneratePlay(StringBuilder sb, int totalRooms)
        {
            int subActions = rng.Next(1, 10);

            for (int i = 0; i < subActions; i++)
            {
                int action = rng.Next(1, 6); // Move/Fight/Pickup/etc.
                bool success = false;
                switch (action)
                {
                    case 1: // Move
                        int dir = rng.Next(0, 4);

                        var (x, y) = roomPositions[currentRoom];
                        (int dx, int dy) = dir switch
                        {
                            0 => (0, -1),
                            1 => (0, 1),
                            2 => (-1, 0),
                            3 => (1, 0),
                            _ => (0, 0)
                        };
                        var next = (x + dx, y + dy);
                        ITester.AppendLine(sb, action.ToString(), nameof(GeneratePlay) + $" Choice");
                        if (!roomHasMonster.TryGetValue(currentRoom, out bool hasMonster) || !hasMonster)
                        {
                            ITester.AppendLine(sb, dir.ToString(), nameof(GeneratePlay) + " Direction");
                            currentRoom = posToRoom.GetValueOrDefault(next, currentRoom);
                        }

                        break;

                    case 2: // Fight
                        if (roomHasMonster.TryGetValue(currentRoom, out bool hasMonsterFight) && hasMonsterFight)
                        {
                            success = true;
                            roomHasMonster[currentRoom] = false;
                            if (!defeatedMonstersRooms.Contains(currentRoom))
                                defeatedMonstersRooms.Add(currentRoom);
                        }
                        ITester.AppendLine(sb, action.ToString(), nameof(GeneratePlay) + $" Choice {success}");
                        break;

                    case 3: // Pickup
                        
                        if ((!roomHasMonster.TryGetValue(currentRoom, out bool monster) ||
                            !monster) && roomHasItem.TryGetValue(currentRoom, out bool hasItem) && hasItem)
                        {
                            success = true;
                            roomHasItem[currentRoom] = false;
                            if (!bagItemRooms.Contains(currentRoom))
                                bagItemRooms.Add(currentRoom);
                        }
                        ITester.AppendLine(sb, action.ToString(), nameof(GeneratePlay) + $" Choice {success}");
                        break;

                    case 4:
                        ITester.AppendLine(sb, action.ToString(), nameof(GeneratePlay) + " Choice");
                        if (bagItemRooms.Count > 0)
                            ITester.AppendLine(sb, rng.Next(1, 4).ToString(), nameof(GeneratePlay) + " Order Modifier");
                        break;

                    case 5:
                        ITester.AppendLine(sb, action.ToString(), nameof(GeneratePlay) + " Choice");
                        if (defeatedMonstersRooms.Count > 0)
                            ITester.AppendLine(sb, rng.Next(1, 4).ToString(), nameof(GeneratePlay) + " Order Modifier");
                        break;
                }
            }
            ITester.AppendLine(sb, "6", nameof(GeneratePlay) + "Quit play"); // Quit play
        }
    }
}
