from time import time
from sklearn.ensemble import RandomForestClassifier
from sklearn.ensemble import AdaBoostClassifier
from sklearn.ensemble import GradientBoostingClassifier
from sklearn.neighbors import KNeighborsClassifier
from sklearn.metrics import accuracy_score
from sklearn.metrics import f1_score


def generateFilename(i):
    _ = ''
    for j in range(6 - len(str(i))):
        _ += '0'
    _ += str(i) + '.png'
    return _


def generatePackageName(x):
    return '000' if x == 0 else '0' + str(x) if 0 < x < 100 else '100' if x == 100 else ''

tileSize = 48
contrastLevel = 100
dataPath = '../Data/Tiles/' + str(tileSize) + '/'
trainDirectoryPath = dataPath + 'Train/' + str(generatePackageName(contrastLevel)) + '/'
testDirectoryPath = dataPath + 'Test/' + str(generatePackageName(contrastLevel)) + '/'

# read trainData
print('read train')
trainX = [[float(number) for number in line.split(' ') if number != '']
          for line in open(trainDirectoryPath + 'features.txt').read().split('\n') if line != '']
trainY = [int(line) for line in open(trainDirectoryPath + 'answers.txt').read().split('\n') if line != '']

print('fitting')
model = RandomForestClassifier(n_estimators=50)
model.fit(trainX, trainY)
trainX = None
trainY = None

# read test data
print('read test')
testX = [tuple([float(number) for number in line.split(' ') if number != ''])
         for line in open(testDirectoryPath + 'features.txt').read().split('\n') if line != '']
testY = [int(line) for line in open(testDirectoryPath + 'answers.txt').read().split('\n') if line != '']

t = time()
print('predicting')
result = model.predict(testX)
print(time() - t)

print(accuracy_score(testY, result))
print(f1_score(testY, result, average='macro'))

errorFile = open(testDirectoryPath + "errors.txt", "w")
table = [[0 for j in range(3)] for i in range(3)]
print('finished')
for i in range(len(result)):
    table[testY[i]][result[i]] += 1
    if result[i] != testY[i]:
        # print(str(i) + ": correct - " + str(testY[i]) + "; classified as " + str(result[i]))
        errorFile.write(generateFilename(i) + ' ' + str(testY[i]) + ' ' + str(result[i]) + '\n')
print(table)

#########################################
# Общая схема
#########################################
# I. Генерация удобных входных данных:
# 1) получаем на вход папку, в которой лежит n изображений и n xml файлов с разметкой вида filename.xml,
# где filename - имя изображения с расширением
# 2) генерируем по файлу и разметке текстуру, в которой для каждого пикселя указаны степень принадлежности баркоду
# 3) выгружаем на диск с суффиксом _BAR.png вместо .xml
# II. Обучение:
# 1) считать все изображения из папки вместе с bar-текстурами
# 2) разбить на тайлы 48x48 и 64x64 пикселей, для каждого из тайлов получить ответ из текстуры, отсеять лишние
# 3) обучить 4 классификатора - под тайлы 48x48 и 64x64 с использованием RandomForest и GradientBoosting.
#   возможно, придётся обучать классификатор отдельно для одномерных и для двумерных баркодов
# 4) при возможности сохранить данные обучения на диск (*)
# III. Классификация:
# 1) считать все изображения из папки
# 2) сгенерировать мипмапы. эвристически количество мипмапов взято равным 5
# 3) разбить каждый мипмап на тайлы 48x48 и 64x64, сопоставляя каждому тайлу область на исходном изображении
# 4) запустить классификацию каждого тайла, обновляя после классификации каждого тайла информацию о
#   содержании баркода в соответствующей области на изображении
# 5) сгенерировать по полученным областям bar-текстуры
# IV. Дополнительные задачи:
# 1) научиться визуализировать признаки
# 2) создать приложение для визуалзиации изображений с наложенными bar-текстурами
# 3) запилить active learning - при неправильной классификации дообучать алгоритм
# 4) при сохранённых на диск результатах обучения можно написать программу для разметки выборки,
#   которая будет автоматически предлагать свои варианты разметки
