const fs = require('fs');
const pdf = require('pdf-parse');

async function run() {
    console.log('Reading project.pdf...');
    let dataBuffer = fs.readFileSync('project.pdf');
    console.log('Instantiating PDFParse...');
    const parser = new pdf.PDFParse({ data: dataBuffer });
    
    try {
        console.log('Extracting text...');
        const result = await parser.getText();
        
        let textContent = '';
        if (result) {
            console.log('Result constructor:', result.constructor.name);
            console.log('Result keys:', Object.keys(result));
            console.log('Result prototype methods:', Object.getOwnPropertyNames(Object.getPrototypeOf(result)));
            
            if (typeof result === 'string') {
                textContent = result;
            } else if (result.text) {
                textContent = result.text;
            } else if (typeof result.toString === 'function' && result.toString !== Object.prototype.toString) {
                textContent = result.toString();
            } else {
                // If it's a custom class, let's try page-by-page using getPageText
                try {
                    const info = await parser.getInfo();
                    console.log('PDF Info:', info);
                    const totalPages = info.total || 0;
                    console.log(`Total Pages: ${totalPages}`);
                    let pagesText = [];
                    for (let i = 1; i <= totalPages; i++) {
                        const pageText = await parser.getPageText(i);
                        pagesText.push(pageText);
                    }
                    textContent = pagesText.join('\n');
                } catch (pageErr) {
                    console.log('Failed to get page-by-page text:', pageErr);
                    textContent = JSON.stringify(result, null, 2);
                }
            }
        }
        
        fs.writeFileSync('project_text.txt', textContent || 'Empty Text');
        console.log('Successfully wrote text to project_text.txt. Length:', textContent.length);
    } catch (e) {
        console.log('Error during getText:', e);
    } finally {
        await parser.destroy();
    }
}

run();





