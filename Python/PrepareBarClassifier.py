from sklearn.ensemble import RandomForestClassifier
from sklearn.ensemble import GradientBoostingClassifier
from pickle import dump


def generateName(x):
    return '000' if x == 0 else '0' + str(x) if 0 < x < 100 else '100' if x == 100 else ''

tileSizes = [48, 64]
contrastLevels = [0, 100]
models = [RandomForestClassifier(n_estimators=100)]

for tileSize in tileSizes:
    for contrastLevel in contrastLevels:
        dataPath = '../Data/Tiles/' + str(tileSize) + '/'
        trainDirectoryPath = dataPath + 'Train/' + generateName(contrastLevel) + '/'

        print('reading', tileSize, contrastLevel)
        trainX = [tuple([float(number) for number in line.split(' ') if number != ''])
                  for line in open(trainDirectoryPath + 'features.txt').read().split('\n') if line != '']
        trainY = [int(line) for line in open(trainDirectoryPath + 'answers.txt').read().split('\n') if line != '']

        for i in range(len(models)):
            print('fitting ' + ('RandomForest' if i == 0 else 'GradientBoosting'))
            models[i].fit(trainX, trainY)

            modelDumpFile = open('Classifiers/' + ('RF' if i == 0 else 'GB') + '_C' + generateName(contrastLevel) + '_' + str(tileSize) + '.model', 'wb')
            dump(models[i], modelDumpFile)
            modelDumpFile.close()
        trainX = None
        trainY = None
