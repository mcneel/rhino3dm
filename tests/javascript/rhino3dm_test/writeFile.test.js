const writeFile = require('./writeFile')

test('writeFile', async () => {
    const result = await writeFile()
    expect(result).toBe(true)
    }
)