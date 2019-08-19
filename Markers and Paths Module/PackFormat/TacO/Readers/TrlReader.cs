﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;

namespace Markers_and_Paths_Module.PackFormat.TacO.Readers {

    public static class TrlReader {

        public static List<Prototypes.PrototypeTrailSection> ReadStream(Stream trlStream) {
            var trlSections = new List<Prototypes.PrototypeTrailSection>();

            // Ensure this stream can seek
            using (var srcStream = trlStream.CanSeek ? trlStream : trlStream.ReplaceWithMemoryStream()) {
                // 32 bit, little-endian
                using (var trlReader = new BinaryReader(srcStream, Encoding.ASCII)) {
                    // If at end of stream, or if stream is 0 length, give up
                    if (trlReader.PeekChar() == -1) return trlSections;

                    // First four bytes are just 0000 to signify the first path section
                    trlReader.ReadInt32();

                    int mapId = trlReader.ReadInt32();

                    var trailPoints = new List<Vector3>();

                    while (trlReader.PeekChar() != -1) {
                        float x = trlReader.ReadSingle();
                        float z = trlReader.ReadSingle();
                        float y = trlReader.ReadSingle();

                        if (z == 0 && x == 0 && y == 0) {
                            trlSections.Add(new Prototypes.PrototypeTrailSection(mapId, trailPoints.ToArray()));
                            trailPoints.Clear();
                        } else {
                            trailPoints.Add(new Vector3(x, y, z));
                        }
                    }

                    if (trailPoints.Any()) {
                        // Record the last trail segment
                        trlSections.Add(new Prototypes.PrototypeTrailSection(mapId, trailPoints.ToArray()));
                    }
                }
            }

            return trlSections;
        }

        public static List<Prototypes.PrototypeTrailSection> ReadBytes(byte[] rawTrlData) {
            return ReadStream(new MemoryStream(rawTrlData));
        }

        public static List<Prototypes.PrototypeTrailSection> ReadFile(string trlPath) {
            if (!File.Exists(trlPath)) {
                Console.WriteLine("No trl file found at " + trlPath);
                return new List<Prototypes.PrototypeTrailSection>();
            }

            return ReadStream(new FileStream(trlPath, FileMode.Open));
        }

    }
}
