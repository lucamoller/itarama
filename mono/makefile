.PHONY: clean build

SOURCE = \
        anglemodel.cs \
        collisionsimulator.cs \
	controller.cs \
        exercisecode.cs \
        flags.cs \
	logger.cs \
	message.cs \
	model.cs \
	parser.cs \
        simulator.cs \
        speedmodel.cs \
        statistics.cs \
        strategy.cs \
        survivalsimulator.cs \
        switchcontroller.cs \
        trackmodel.cs \
        throttlecontroller.cs \
        turbosimulator.cs \

all: clean build

clean:
	rm -rf bot.exe replay.exe

build: bot.exe replay.exe 

bot.exe: bot.cs 
	gmcs -out:bot.exe -r:Newtonsoft.Json.dll bot.cs $(SOURCE)

replay.exe:  replay.cs 
	gmcs -out:replay.exe -r:Newtonsoft.Json.dll replay.cs $(SOURCE)
