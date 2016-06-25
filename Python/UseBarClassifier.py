from time import time
from pickle import load
import sys

def generateName(x):
    return '000' if x == 0 else '0' + str(x) if 0 < x < 100 else '100' if x == 100 else ''


modelPath = sys.argv[1]
# modelPath = 'C:/work/barcodes/python/classifiers/'
dataPath = sys.argv[2]
# dataPath = 'C:/work/barcodes/data/finalstage/'

sizes = [64]
contrastLevels = [0, 100]
prefixes = ['RF']

for size in sizes:
    for contrastLevel in contrastLevels:
        packageName = 'C' + generateName(contrastLevel) + '_' + str(size)
        modelFiles = [open(modelPath + '/' + prefix + '_' + packageName + '.model', 'rb') for prefix in prefixes]
        models = [load(file) for file in modelFiles]
        for file in modelFiles:
            file.close()

        print(packageName + ': read')
        x = [tuple([float(number) for number in line.split(' ') if number != ''])
             for line in
             open(dataPath + '/features_' + str(contrastLevel) + '_' + str(size) + '.txt').read().split('\n') if
             line != '']

        print(packageName + ': predicting')
        t = time()
        result = models[0].predict(x)
        print(time() - t)

        answersFile = open(dataPath + '/answers_' + str(contrastLevel) + '_' + str(size) + '.txt', 'w')
        for answer in result:
            answersFile.write(str(answer) + '\n')
        answersFile.close()
        print(packageName + ': finished')
