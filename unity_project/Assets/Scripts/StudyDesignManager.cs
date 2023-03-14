using System;
using UnityEngine;

namespace Assets.Scripts
{
    public class StudyDesignManager
    {
        private readonly ConditionDescription[][] balancedLatinSquareDesign = {
            new []{C(0b000), C(0b111), C(0b011), C(0b001), C(0b100), C(0b110), C(0b101), C(0b010)},
            new []{C(0b111), C(0b001), C(0b000), C(0b110), C(0b011), C(0b010), C(0b100), C(0b101)},
            new []{C(0b001), C(0b110), C(0b111), C(0b010), C(0b000), C(0b101), C(0b011), C(0b100)},
            new []{C(0b110), C(0b010), C(0b001), C(0b101), C(0b111), C(0b100), C(0b000), C(0b011)},
            new []{C(0b010), C(0b101), C(0b110), C(0b100), C(0b001), C(0b011), C(0b111), C(0b000)},
            new []{C(0b101), C(0b100), C(0b010), C(0b011), C(0b110), C(0b000), C(0b001), C(0b111)},
            new []{C(0b100), C(0b011), C(0b101), C(0b000), C(0b010), C(0b111), C(0b110), C(0b001)},
            new []{C(0b011), C(0b000), C(0b100), C(0b111), C(0b101), C(0b001), C(0b010), C(0b110)},
        };

        private static ConditionDescription C(int binaryFlags)
        {
            return new ConditionDescription(
                (binaryFlags & 0b100) == 0b100,
                (binaryFlags & 0b010) == 0b010,
                (binaryFlags & 0b001) == 0b001
            );
        }
        
        public ConditionDescription[] GetCurrentBalancedLatinSquare(int participantId)
        {
            if (participantId < 1)
            {
                UnityEditor.EditorApplication.ExitPlaymode();
                throw new Exception("Participant ID must be larger or equal to 1.");
            }
            int rowNumber = (participantId-1) % balancedLatinSquareDesign.Length;
            Debug.Log($"Using balanced latin square row #{rowNumber+1} for participant {participantId}");
            return balancedLatinSquareDesign[rowNumber];
        }
        
    }

    public class ConditionDescription
    {
        public ConditionDescription(bool hasAuditive, bool hasTactile, bool hasVisual)
        {
            HasAuditive = hasAuditive;
            HasTactile = hasTactile;
            HasVisual = hasVisual;
        }

        public bool HasAuditive { get; }

        public bool HasTactile { get; }

        public bool HasVisual { get; }
    }
}