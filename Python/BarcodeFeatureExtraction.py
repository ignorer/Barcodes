def _checkTile(tile):
    if len(tile) == 0:
        return False
    for row in tile:
        if len(row) != len(tile[0]):
            return False
    return True


def extractStandardDeviationFeatures(tile, horizontalPartCount=2, verticalPartCount=2):
    if not _checkTile(tile):
        raise Exception("Incorrect tile sizes")
    if len(tile) % verticalPartCount != 0 or len(tile[0]) % horizontalPartCount != 0:
        raise Exception("Incorrect partCount")

    features = []
    n = len(tile)
    m = len(tile[0])
    for vIndex in range(verticalPartCount):
        for hIndex in range(horizontalPartCount):
            # find average
            average = 0
            for i in range(n // verticalPartCount):
                for j in range(m // horizontalPartCount):
                    average += tile[i + n // verticalPartCount * vIndex][j + m // horizontalPartCount * hIndex]
            average /= n * m

            # find deviation
            std = 0
            for i in range(n // verticalPartCount):
                for j in range(m // horizontalPartCount):
                    std += (tile[i + n // verticalPartCount * vIndex]
                            [j + m // horizontalPartCount * hIndex] - average) ** 2
            std = (std / (n * m)) ** (1 / 2)
            features.append(std)

    return features


def extractStructureTensorFeatures(tile, horizontalPartCount=2, verticalPartCount=2):
    if not _checkTile(tile):
        raise Exception("Incorrect tile sizes")
    if len(tile) % verticalPartCount != 0 or len(tile[0]) % horizontalPartCount != 0:
        raise Exception("Incorrect partCount")

    # eigenvalues
    e1 = 0
    e2 = 0
    # sizes
    n = len(tile)
    m = len(tile[0])

    for vIndex in range(verticalPartCount):
        for hIndex in range(horizontalPartCount):
            # matrix
            T11 = T12 = T22 = 0
            for i in range(1, n // verticalPartCount - 1):
                for j in range(1, m // horizontalPartCount - 1):
                    # derivatives
                    Ix = tile[i][j + 1] - tile[i][j - 1]
                    Iy = tile[i + 1][j] - tile[i - 1][j]
                    T11 += Ix * Ix
                    T12 += Ix * Iy
                    T22 += Iy * Iy

            # find eigenvalues
            e1 += (T11 + T22 - ((T11 - T22) ** 2 - 4 * T12 * T12) ** (1/2)) / 2
            e2 += (T11 + T22 + ((T11 - T22) ** 2 - 4 * T12 * T12) ** (1/2)) / 2

    return [min(abs(e1), abs(e2)), max(abs(e1), abs(e2))]


def extractLocalBinaryPatternFeatures(tile):
    if not _checkTile(tile):
        raise Exception("Incorrect tile sizes")
    histogram = [0 for i in range(256)]
    n = len(tile)
    m = len(tile[0])
    for i in range(1, n - 1):
        for j in range(1, m - 1):
            pattern = [0 for i in range(8)]
            pattern[0] = 1 if tile[i - 1][j - 1] > tile[i][j] else 0
            pattern[1] = 1 if tile[i - 1][j] > tile[i][j] else 0
            pattern[2] = 1 if tile[i - 1][j + 1] > tile[i][j] else 0
            pattern[3] = 1 if tile[i][j - 1] > tile[i][j] else 0
            pattern[4] = 1 if tile[i][j + 1] > tile[i][j] else 0
            pattern[5] = 1 if tile[i + 1][j - 1] > tile[i][j] else 0
            pattern[6] = 1 if tile[i + 1][j] > tile[i][j] else 0
            pattern[7] = 1 if tile[i + 1][j + 1] > tile[i][j] else 0

            index = 0
            for x in pattern:
                index = index * 2 + x

            histogram[index] += 1
    return histogram


def extractAllFeatures(tile):
    return \
        extractStandardDeviationFeatures(tile) + \
        extractLocalBinaryPatternFeatures(tile) + \
        extractStructureTensorFeatures(tile)


def eigenVectors(tile, horizontalPartCount=2, verticalPartCount=2):
    if not _checkTile(tile):
        raise Exception("Incorrect tile sizes")
    if len(tile) % verticalPartCount != 0 or len(tile[0]) % horizontalPartCount != 0:
        raise Exception("Incorrect partCount")

    # sizes
    n = len(tile)
    m = len(tile[0])

    for vIndex in range(verticalPartCount):
        for hIndex in range(horizontalPartCount):
            # matrix
            T11 = T12 = T22 = 0
            # eigenvalues
            e1 = e2 = 0
            for i in range(1, n // verticalPartCount - 1):
                for j in range(1, m // horizontalPartCount - 1):
                    # derivatives
                    Ix = tile[i][j + 1] - tile[i][j - 1]
                    Iy = tile[i + 1][j] - tile[i - 1][j]
                    T11 += Ix * Ix
                    T12 += Ix * Iy
                    T22 += Iy * Iy

            # find eigenvalues
            e1 += (T11 + T22 - ((T11 - T22) ** 2 - 4 * T12 * T12) ** (1/2)) / 2
            e2 += (T11 + T22 + ((T11 - T22) ** 2 - 4 * T12 * T12) ** (1/2)) / 2

            print(vIndex, hIndex, e1, [1, abs((e1-T11) / T12)], e2, [abs((e2-T22) / T12), 1])
