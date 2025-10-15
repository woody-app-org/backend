package com.woody.backend.controller;

import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.woody.backend.domain.dto.HelloDto;
import com.woody.backend.domain.service.interfaces.HelloService;

@RestController
@RequestMapping("/")
public class HelloController {
    @Autowired
    private HelloService helloService;

    public HelloController() {

    }

    @GetMapping()
    public ResponseEntity<List<HelloDto>> getHellos(){
        var hellos = this.helloService.getHello();

        return new ResponseEntity<List<HelloDto>>(hellos, HttpStatus.OK);
    }
}
