using CookbookTheMealDB;

namespace CookbookTests.TheMealDB;

public class MeasureParserTests
{
    [Fact]
    public void ParseMeasure_WhenEmptyString_ReturnsDefault()
    {
        var result = MeasureParser.ParseMeasure("");

        result.Quantity.Should().BeNull();
        result.Unit.Should().Be("гр");
    }

    [Fact]
    public void ParseMeasure_WhenNull_ReturnsDefault()
    {
        var result = MeasureParser.ParseMeasure(null);

        result.Quantity.Should().BeNull();
        result.Unit.Should().Be("гр");
    }

    [Fact]
    public void ParseMeasure_WhenWhitespace_ReturnsDefault()
    {
        var result = MeasureParser.ParseMeasure("   ");

        result.Quantity.Should().BeNull();
        result.Unit.Should().Be("гр");
    }

    [Fact]
    public void ParseMeasure_WhenToTaste_ReturnsNullQuantity()
    {
        var result = MeasureParser.ParseMeasure("to taste");

        result.Quantity.Should().BeNull();
        result.Unit.Should().Be("по вкусу");
    }

    [Fact]
    public void ParseMeasure_WhenAsNeeded_ReturnsNullQuantity()
    {
        var result = MeasureParser.ParseMeasure("as needed");

        result.Quantity.Should().BeNull();
        result.Unit.Should().Be("по вкусу");
    }

    [Fact]
    public void ParseMeasure_WhenPinch_ReturnsNullQuantity()
    {
        var result = MeasureParser.ParseMeasure("pinch");

        result.Quantity.Should().BeNull();
        result.Unit.Should().Be("по вкусу");
    }

    [Fact]
    public void ParseMeasure_WhenSimpleNumber_ReturnsQuantity()
    {
        var result = MeasureParser.ParseMeasure("100");

        result.Quantity.Should().Be(100);
        result.Unit.Should().Be("гр");
    }

    [Fact]
    public void ParseMeasure_WhenNumberWithUnit_ReturnsQuantityAndUnit()
    {
        var result = MeasureParser.ParseMeasure("100 g");

        result.Quantity.Should().Be(100);
        result.Unit.Should().Be("гр");
    }


    [Fact]
    public void ParseMeasure_WhenDecimalNumber_ReturnsQuantity()
    {
        var result = MeasureParser.ParseMeasure("1.5 kg");

        result.Quantity.Should().Be(1.5m);
        result.Unit.Should().Be("кг");
    }

    [Fact]
    public void ParseMeasure_WhenTablespoon_ReturnsCorrectUnit()
    {
        var result = MeasureParser.ParseMeasure("2 tbsp");

        result.Quantity.Should().Be(2);
        result.Unit.Should().Be("ст.л");
    }

    [Fact]
    public void ParseMeasure_WhenTeaspoon_ReturnsCorrectUnit()
    {
        var result = MeasureParser.ParseMeasure("1 tsp");

        result.Quantity.Should().Be(1);
        result.Unit.Should().Be("ч.л");
    }

    [Fact]
    public void ParseMeasure_WhenRussianUnit_ReturnsCorrectUnit()
    {
        var result = MeasureParser.ParseMeasure("200 грамм");

        result.Quantity.Should().Be(200);
        result.Unit.Should().Be("гр");
    }
}

