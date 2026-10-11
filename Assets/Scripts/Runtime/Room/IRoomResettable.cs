namespace Game.Room
{
    /// <summary>
    /// Anything in a room whose state returns to its start when the room resets. All resets go
    /// through <see cref="RoomController.ResetRoom"/>, which is also where a future "player was
    /// caught" animation will end up (Horror mode).
    /// </summary>
    public interface IRoomResettable
    {
        void ResetState();
    }
}
