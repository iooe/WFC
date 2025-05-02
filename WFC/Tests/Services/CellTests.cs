using Microsoft.VisualStudio.TestTools.UnitTesting;
using WFC.Models;

namespace WFC.Tests.Models;

[TestClass]
public class CellTests
{
    [TestMethod]
    public void Constructor_WithNumberOfStates_InitializesCorrectly()
    {
        // Arrange & Act
        var cell = new Cell(5);
        
        // Assert
        Assert.AreEqual(5, cell.PossibleStates.Count);
        Assert.IsFalse(cell.Collapsed);
        Assert.IsNull(cell.CollapsedState);
        Assert.IsTrue(cell.Entropy > 0);
    }
    
    [TestMethod]
    public void Collapse_ToValidState_SetsCorrectState()
    {
        // Arrange
        var cell = new Cell(5);
        
        // Act
        cell.Collapse(2);
        
        // Assert
        Assert.IsTrue(cell.Collapsed);
        Assert.AreEqual(2, cell.CollapsedState);
        Assert.AreEqual(0, cell.Entropy);
        Assert.AreEqual(1, cell.PossibleStates.Count);
        Assert.IsTrue(cell.PossibleStates.Contains(2));
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Collapse_ToInvalidState_ThrowsException()
    {
        // Arrange
        var cell = new Cell(3); // States 0,1,2
        
        // Act & Assert (should throw)
        cell.Collapse(5); // Invalid state
    }
    
    [TestMethod]
    public void Constrain_RemovesStates_ReturnsTrue()
    {
        // Arrange
        var cell = new Cell(5); // States 0,1,2,3,4
        
        // Act
        bool changed = cell.Constrain(s => s % 2 == 0); // Keep only even states
        
        // Assert
        Assert.IsTrue(changed);
        Assert.AreEqual(3, cell.PossibleStates.Count); // 0,2,4 remain
        Assert.IsTrue(cell.PossibleStates.Contains(0));
        Assert.IsTrue(cell.PossibleStates.Contains(2));
        Assert.IsTrue(cell.PossibleStates.Contains(4));
        Assert.IsFalse(cell.PossibleStates.Contains(1));
        Assert.IsFalse(cell.PossibleStates.Contains(3));
    }
    
    [TestMethod]
    public void Constrain_NoStateChanges_ReturnsFalse()
    {
        // Arrange
        var cell = new Cell(3); // States 0,1,2
        
        // Act
        bool changed = cell.Constrain(s => true); // Keep all states
        
        // Assert
        Assert.IsFalse(changed);
        Assert.AreEqual(3, cell.PossibleStates.Count);
    }
    
    [TestMethod]
    public void ConstrainToStates_WithValidStates_ConstrainsCorrectly()
    {
        // Arrange
        var cell = new Cell(5); // States 0,1,2,3,4
        
        // Act
        bool changed = cell.ConstrainToStates(new[] { 1, 3 });
        
        // Assert
        Assert.IsTrue(changed);
        Assert.AreEqual(2, cell.PossibleStates.Count);
        Assert.IsTrue(cell.PossibleStates.Contains(1));
        Assert.IsTrue(cell.PossibleStates.Contains(3));
    }
}