using System;
using Musician_Module.Controls.Instrument;
using Musician_Module.Notation.Parsers;
using Musician_Module.Notation.Persistance;
using Musician_Module.Player.Algorithms;
using System.Collections.Generic;
using Blish_HUD.Controls.Intern;
using Blish_HUD;

namespace Musician_Module.Player
{
    internal static class MusicPlayerFactory
    {
        private static Dictionary<string, Instrument> InstrumentRepository = new Dictionary<string, Instrument>()
        {
            { "harp", new Harp() },
            { "flute", new Flute() },
            { "lute", new Lute() },
            { "horn", new Horn() },
            { "bass", new Bass() },
            { "bell", new Bell() },
            { "bell2", new Bell2() },
        };
        internal static MusicPlayer Create(RawMusicSheet rawMusicSheet, InstrumentMode mode)
        {
            return MusicBoxNotationMusicPlayerFactory(rawMusicSheet, mode);
        }
        private static MusicPlayer MusicBoxNotationMusicPlayerFactory(RawMusicSheet rawMusicSheet, InstrumentMode mode)
        {
            var musicSheet = new MusicSheetParser(new ChordParser(new NoteParser(), rawMusicSheet.Instrument)).Parse(
                rawMusicSheet.Melody,
                int.Parse(rawMusicSheet.Tempo),
                int.Parse(rawMusicSheet.Meter.Split('/')[0]),
                int.Parse(rawMusicSheet.Meter.Split('/')[1]));


            var algorithm = rawMusicSheet.Algorithm == "favor notes"
                ? new FavorNotesAlgorithm() : (IPlayAlgorithm)new FavorChordsAlgorithm();

            Instrument instrument = InstrumentRepository[rawMusicSheet.Instrument];
            instrument.Mode = mode;
            MusicianModule.ModuleInstance.Conveyor.Visible = mode == InstrumentMode.Practice;

            return new MusicPlayer(
                musicSheet,
                instrument,
                algorithm);
        }
    }
}