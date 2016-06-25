from matplotlib import pyplot

def generatePackageName(x):
    return '000' if x == 0 else '0' + str(x) if 0 < x < 100 else '100' if x == 100 else ''


tileSize = 64
contrastLevel = 100
path = '../Data/Tiles/' + str(tileSize) + '/Train/' + str(generatePackageName(contrastLevel))
featuresFile = open(path + '/features.txt')
features = [line.split(' ')[-3:-1] for line in featuresFile.read().split('\n') if line != '']
featuresFile.close()
answersFile = open(path + '/answers.txt')
answers = [int(line) for line in answersFile.read().split('\n') if line != '']
answersFile.close()

objects = []
for i in range(len(features)):
    if answers[i] == 0:
        objects.append([float(features[i][0]), float(features[i][1])])
pyplot.plot(*zip(*objects), marker='.', color='black', ls='')

objects = []
for i in range(len(features)):
    if answers[i] == 1:
        objects.append([float(features[i][0]), float(features[i][1])])
pyplot.plot(*zip(*objects), marker='.', color='red', ls='')

objects = []
for i in range(len(features)):
    if answers[i] == 2:
        objects.append([float(features[i][0]), float(features[i][1])])
pyplot.plot(*zip(*objects), marker='.', color='green', ls='')

pyplot.show()
